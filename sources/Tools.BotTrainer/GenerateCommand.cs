using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Data.DartContext.Entities.Bot;
using Logic.Bot.Ml;
using Logic.Bot.Tactics;
using Microsoft.Extensions.DependencyInjection;
using Shared.Enums.Bot;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Bot;
using Shared.Models.Bot.Ml;
using Shared.Models.Options;

namespace Tools.BotTrainer
{
    /// <summary>
    /// Phase 2: Datensatz aus simulierten Situationen (CSV) + Metadaten in DartBotTrainingDatasets.
    /// Mit --policy-model spielt in den Rollouts das Modell (Hybrid) statt des Regelwerks weiter (Training in Runden).
    /// </summary>
    public static class GenerateCommand
    {
        /// <summary>Oberhalb dieses Rests entscheidet in Rollouts mit Modell allein das Regelwerk (Laufzeit).</summary>
        private const int DefaultPolicyMaxRemaining = 170;

        public static async Task<int> RunAsync(Arguments arguments, MlDataStore store, ServiceProvider? provider, JobHooks? hooks = null)
        {
            var situations = arguments.Int("situations", 1000);
            var options = new RolloutOptions
            {
                RolloutsPerCandidate = arguments.Int("rollouts", 50),
                MaxDartsPerLeg = arguments.Int("max-darts", 60),
                Seed = arguments.Int("seed", 1),
                AdaptiveRollouts = !arguments.Flag("fixed-rollouts")
            };

            var policyModelId = arguments.Int("policy-model", 0);
            var policyMargin = arguments.Float("policy-margin", new DartBotOptions().Ml.Margin);
            var policyMaxRemaining = arguments.Int("policy-max-remaining", DefaultPolicyMaxRemaining);
            LoadedTargetModel? policyModel = null;

            if (policyModelId != 0)
            {
                if (provider is null)
                {
                    Console.Error.WriteLine("--policy-model braucht die Datenbank (ohne --no-db).");
                    return 1;
                }

                await using var loadScope = provider.CreateAsyncScope();
                var (_, model, error) = await ModelLoader.LoadAsync(loadScope.ServiceProvider.GetRequiredService<IUnitOfWork>(), store, policyModelId);

                if (model is null)
                {
                    Console.Error.WriteLine(error);
                    return 1;
                }

                policyModel = model;
            }

            var storageKey = $"datasets/target-selection-v{TargetCandidateFeatures.FeatureSchemaVersion}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-seed{options.Seed}" +
                             (policyModelId != 0 ? $"-policy{policyModelId}" : "") + ".csv";
            var path = store.PathOf(storageKey);

            Console.WriteLine($"Erzeuge {situations} Situationen × Kandidaten × {options.RolloutsPerCandidate} Rollouts" +
                              $"{(options.AdaptiveRollouts ? " (nach Rest ×2 / ×0,5)" : "")} (Seed {options.Seed}) → {path}");
            Console.WriteLine(policyModel is null
                ? "Rollouts: Regelwerk"
                : $"Rollouts: Modell {policyModelId} (Schwelle {policyMargin:0.##}, bis Rest {policyMaxRemaining}), sonst Regelwerk");

            var stopwatch = Stopwatch.StartNew();
            var sampler = new SituationSampler(options.Seed);

            using (var writer = new CsvDatasetWriter(path))
            {
                for (var i = 0; i < situations; i++)
                {
                    var situationOptions = new RolloutOptions
                    {
                        RolloutsPerCandidate = options.RolloutsPerCandidate,
                        MaxDartsPerLeg = options.MaxDartsPerLeg,
                        Seed = unchecked(options.Seed * 397 + i),
                        AdaptiveRollouts = options.AdaptiveRollouts
                    };

                    var evaluator = CreateEvaluator(policyModel, policyMargin, policyMaxRemaining);
                    var rows = evaluator.Evaluate(sampler.Next(), situationOptions);

                    foreach (var row in rows)
                    {
                        row.SituationId = i;
                    }

                    writer.Write(rows);

                    if ((i + 1) % 50 == 0 || i + 1 == situations)
                    {
                        var perSituation = stopwatch.Elapsed.TotalMilliseconds / (i + 1);
                        Console.WriteLine($"  {i + 1}/{situations} Situationen, {writer.RowCount} Zeilen, {stopwatch.Elapsed:hh\\:mm\\:ss} ({perSituation:0} ms je Situation)");
                    }

                    if (hooks is not null && ((i + 1) % 10 == 0 || i + 1 == situations))
                    {
                        await hooks.ReportOrCancelAsync($"Datensatz: {i + 1}/{situations} Situationen", (i + 1) * 100 / situations);
                    }
                }
            }

            var file = new FileInfo(path);
            var sha256 = MlDataStore.Sha256Of(path);
            var rowCount = File.ReadLines(path).LongCount() - 1;

            Console.WriteLine($"Fertig: {rowCount} Zeilen, {file.Length / 1024.0 / 1024.0:0.0} MB, SHA-256 {sha256}, {stopwatch.Elapsed:hh\\:mm\\:ss}");

            if (provider is null)
            {
                Console.WriteLine("--no-db: keine Metadaten gespeichert.");
                return 0;
            }

            await using var scope = provider.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var dataset = new DartBotTrainingDatasetEntity
            {
                StorageKey = storageKey,
                Format = DartBotFileFormatEnum.Csv,
                SourceType = DartBotDatasetSourceTypeEnum.Simulation,
                RowCount = rowCount,
                SizeBytes = file.Length,
                Sha256 = sha256,
                FeatureSchemaVersion = TargetCandidateFeatures.FeatureSchemaVersion,
                RandomSeed = options.Seed,
                GeneratorTargetModelId = policyModelId != 0 ? policyModelId : null,
                GeneratorParametersJson = JsonSerializer.Serialize(new
                {
                    situations,
                    options.RolloutsPerCandidate,
                    options.AdaptiveRollouts,
                    options.MaxDartsPerLeg,
                    strategy = policyModelId != 0 ? "HybridTargetSelector" : "RuleBasedTargetSelector",
                    policyModelId = policyModelId != 0 ? policyModelId : (int?)null,
                    policyMargin = policyModelId != 0 ? policyMargin : (float?)null,
                    policyMaxRemaining = policyModelId != 0 ? policyMaxRemaining : (int?)null,
                    sampler = SituationSampler.Describe(),
                    label = nameof(TargetCandidateFeatures.RelativeDarts),
                    commonRandomNumbers = true,
                    durationSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 1)
                })
            };

            await unitOfWork.DartBotUnitOfWork.DartBotTrainingDatasetRepository.AddAsync(dataset);
            await unitOfWork.SaveChangesAsync();

            Console.WriteLine($"Metadaten gespeichert: DartBotTrainingDatasets.Id = {dataset.Id}");

            if (hooks is not null)
            {
                hooks.CreatedId = dataset.Id;
            }

            return 0;
        }

        /// <summary>Je Situation ein eigener Vorhersage-Speicher: gleiche Zustände kommen in den Rollouts sehr oft vor.</summary>
        private static CandidateEvaluator CreateEvaluator(LoadedTargetModel? policyModel, float margin, int maxRemaining)
        {
            if (policyModel is null)
            {
                return new CandidateEvaluator();
            }

            var cache = new ConcurrentDictionary<HybridTargetSelector.StateKey, IReadOnlyDictionary<DartTarget, float>>();

            return new CandidateEvaluator(policyFactory: random =>
                new HybridTargetSelector(policyModel, new RuleBasedTargetSelector(random), margin, cache, maxRemaining));
        }
    }
}
