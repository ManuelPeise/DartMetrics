using System.Diagnostics;
using System.Text.Json;
using Data.DartContext.Entities.Bot;
using Logic.Bot.Ml;
using Microsoft.Extensions.DependencyInjection;
using Shared.Enums.Bot;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Bot.Ml;

namespace Tools.BotTrainer
{
    /// <summary>Phase 2: Datensatz aus simulierten Situationen (CSV) + Metadaten in DartBotTrainingDatasets.</summary>
    public static class GenerateCommand
    {
        public static async Task<int> RunAsync(Arguments arguments, MlDataStore store, ServiceProvider? provider)
        {
            var situations = arguments.Int("situations", 1000);
            var options = new RolloutOptions
            {
                RolloutsPerCandidate = arguments.Int("rollouts", 50),
                MaxDartsPerLeg = arguments.Int("max-darts", 60),
                Seed = arguments.Int("seed", 1)
            };

            var storageKey = $"datasets/target-selection-v{TargetCandidateFeatures.FeatureSchemaVersion}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-seed{options.Seed}.csv";
            var path = store.PathOf(storageKey);

            Console.WriteLine($"Erzeuge {situations} Situationen × Kandidaten × {options.RolloutsPerCandidate} Rollouts (Seed {options.Seed}) → {path}");

            var stopwatch = Stopwatch.StartNew();
            var sampler = new SituationSampler(options.Seed);
            var evaluator = new CandidateEvaluator();

            using (var writer = new CsvDatasetWriter(path))
            {
                for (var i = 0; i < situations; i++)
                {
                    var situationOptions = new RolloutOptions
                    {
                        RolloutsPerCandidate = options.RolloutsPerCandidate,
                        MaxDartsPerLeg = options.MaxDartsPerLeg,
                        Seed = unchecked(options.Seed * 397 + i)
                    };

                    writer.Write(evaluator.Evaluate(sampler.Next(), situationOptions));

                    if ((i + 1) % 50 == 0 || i + 1 == situations)
                    {
                        Console.WriteLine($"  {i + 1}/{situations} Situationen, {writer.RowCount} Zeilen, {stopwatch.Elapsed:hh\\:mm\\:ss}");
                    }
                }
            }

            var file = new FileInfo(path);
            var sha256 = MlDataStore.Sha256Of(path);
            var rowCount = File.ReadLines(path).LongCount() - 1;

            Console.WriteLine($"Fertig: {rowCount} Zeilen, {file.Length / 1024.0 / 1024.0:0.0} MB, SHA-256 {sha256}");

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
                GeneratorParametersJson = JsonSerializer.Serialize(new
                {
                    situations,
                    options.RolloutsPerCandidate,
                    options.MaxDartsPerLeg,
                    strategy = "RuleBasedTargetSelector",
                    label = nameof(TargetCandidateFeatures.ExpectedDartsToFinish)
                })
            };

            await unitOfWork.DartBotUnitOfWork.DartBotTrainingDatasetRepository.AddAsync(dataset);
            await unitOfWork.SaveChangesAsync();

            Console.WriteLine($"Metadaten gespeichert: DartBotTrainingDatasets.Id = {dataset.Id}");
            return 0;
        }
    }
}
