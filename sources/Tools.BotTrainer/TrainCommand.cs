using Logic.Bot.Ml;
using System.Diagnostics;
using System.Text.Json;
using Data.DartContext.Entities.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;
using Shared.Enums.Bot;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Bot.Ml;

namespace Tools.BotTrainer
{
    /// <summary>
    /// Phase 3: Zielwahl-Modell aus einem Datensatz trainieren.
    /// - Datensatz über seine Id laden, SHA-256 und FeatureSchemaVersion prüfen.
    /// - Lauf als DartBotModelTrainingEntity (Running → Completed/Failed), auch Fehlschläge bleiben sichtbar.
    /// - FastTree-Regression auf RelativeDarts; Aufteilung Training/Test nach Situationen (alle Kandidaten einer
    ///   Situation landen auf derselben Seite), fester Seed.
    /// - Modell als ZIP unter ml_data/models (nie überschrieben) + DartBotModelEntity. Aktiviert wird nichts.
    /// </summary>
    public static class TrainCommand
    {
        /// <summary>Schema 2: Abstand zum besten Kandidaten der Situation (die Rangfolge zählt, nicht der absolute Wert).</summary>
        private const string Label = nameof(TargetCandidateRow.RelativeDarts);

        public static async Task<int> RunAsync(Arguments arguments, MlDataStore store, ServiceProvider provider, JobHooks? hooks = null)
        {
            var datasetId = arguments.Int("dataset", 0);

            if (datasetId == 0)
            {
                Console.Error.WriteLine("--dataset <Id> fehlt (Id aus DartBotTrainingDatasets).");
                return 1;
            }

            var seed = arguments.Int("seed", 1);
            var hyperparameters = new
            {
                NumberOfTrees = arguments.Int("trees", 200),
                NumberOfLeaves = arguments.Int("leaves", 64),
                MinimumExampleCountPerLeaf = arguments.Int("min-leaf", 20),
                LearningRate = 0.1,
                TestFraction = 0.2
            };

            await using var scope = provider.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var bots = unitOfWork.DartBotUnitOfWork;

            var dataset = await bots.DartBotTrainingDatasetRepository.GetFirstOrDefaultAsync(d => d.Id == datasetId);

            if (dataset is null)
            {
                Console.Error.WriteLine($"Datensatz {datasetId} nicht gefunden.");
                return 1;
            }

            var datasetPath = store.PathOf(dataset.StorageKey, mustBeNew: false);

            if (!File.Exists(datasetPath))
            {
                Console.Error.WriteLine($"Datei fehlt: {datasetPath}");
                return 1;
            }

            if (dataset.FeatureSchemaVersion != TargetCandidateFeatures.FeatureSchemaVersion)
            {
                Console.Error.WriteLine($"Datensatz hat FeatureSchemaVersion {dataset.FeatureSchemaVersion}, erwartet {TargetCandidateFeatures.FeatureSchemaVersion}.");
                return 1;
            }

            Console.WriteLine($"Prüfe SHA-256 von {datasetPath} …");

            if (!string.Equals(MlDataStore.Sha256Of(datasetPath), dataset.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("SHA-256 stimmt nicht mit den Metadaten überein – Datei wurde verändert.");
                return 1;
            }

            var mlNetVersion = typeof(MLContext).Assembly.GetName().Version?.ToString() ?? "unbekannt";

            // Lauf zuerst anlegen: auch ein Fehlschlag ist danach nachvollziehbar.
            var training = new DartBotModelTrainingEntity
            {
                ModelType = DartBotModelTypeEnum.TargetSelection,
                Dataset = dataset,
                Status = DartBotModelTrainingStatusEnum.Running,
                StartedAt = DateTime.UtcNow,
                TrainerName = "FastTree (Regression)",
                RandomSeed = seed,
                MlNetVersion = mlNetVersion,
                CodeVersion = Environment.GetEnvironmentVariable("BOTTRAINER_COMMIT"),
                FeatureSchemaVersion = TargetCandidateFeatures.FeatureSchemaVersion,
                HyperparametersJson = JsonSerializer.Serialize(hyperparameters)
            };

            await bots.DartBotModelTrainingRepository.AddAsync(training);
            await unitOfWork.SaveChangesAsync();

            Console.WriteLine($"Trainingslauf {training.Id}: Datensatz {dataset.Id} ({dataset.RowCount:N0} Zeilen), Seed {seed}");

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var ml = new MLContext(seed);

                var data = ml.Data.LoadFromTextFile<TargetCandidateRow>(datasetPath, separatorChar: ',', hasHeader: true);
                var split = ml.Data.TrainTestSplit(data, testFraction: hyperparameters.TestFraction,
                    samplingKeyColumnName: nameof(TargetCandidateRow.SituationId), seed: seed);

                var pipeline = ml.Transforms.Categorical.OneHotEncoding(
                        [
                            new InputOutputColumnPair("SegmentOneHot", nameof(TargetCandidateRow.CandidateSegment)),
                            new InputOutputColumnPair("MultiplierOneHot", nameof(TargetCandidateRow.CandidateMultiplier))
                        ])
                    .Append(ml.Transforms.Concatenate("Features",
                        [.. TargetCandidateRow.NumericFeatures, "SegmentOneHot", "MultiplierOneHot"]))
                    .Append(ml.Regression.Trainers.FastTree(new FastTreeRegressionTrainer.Options
                    {
                        LabelColumnName = Label,
                        FeatureColumnName = "Features",
                        NumberOfTrees = hyperparameters.NumberOfTrees,
                        NumberOfLeaves = hyperparameters.NumberOfLeaves,
                        MinimumExampleCountPerLeaf = hyperparameters.MinimumExampleCountPerLeaf,
                        LearningRate = hyperparameters.LearningRate
                    }));

                Console.WriteLine("Trainiere …");
                var model = pipeline.Fit(split.TrainSet);

                var metrics = ml.Regression.Evaluate(model.Transform(split.TestSet), labelColumnName: Label);
                var trainCount = CountRows(ml, split.TrainSet);
                var topChoice = TopChoiceAccuracy(ml, model.Transform(split.TestSet));

                Console.WriteLine($"Fertig nach {stopwatch.Elapsed:hh\\:mm\\:ss}: RMSE {metrics.RootMeanSquaredError:0.000}, MAE {metrics.MeanAbsoluteError:0.000}, R² {metrics.RSquared:0.0000}");
                Console.WriteLine($"Zielwahl auf Testsituationen: bestes Ziel getroffen {topChoice.HitRate:P1}, mittlerer Verlust {topChoice.MeanRegret:0.000} Darts");

                var storageKey = $"models/target-selection-v{TargetCandidateFeatures.FeatureSchemaVersion}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-training{training.Id}.zip";
                var modelPath = store.PathOf(storageKey);
                ml.Model.Save(model, data.Schema, modelPath);

                var versionNumber = await NextVersionAsync(bots.DartBotModelRepository, DartBotModelTypeEnum.TargetSelection);

                var modelEntity = new DartBotModelEntity
                {
                    ModelType = DartBotModelTypeEnum.TargetSelection,
                    VersionNumber = versionNumber,
                    Training = training,
                    StorageKey = storageKey,
                    Format = DartBotFileFormatEnum.MlNetZip,
                    SizeBytes = new FileInfo(modelPath).Length,
                    Sha256 = MlDataStore.Sha256Of(modelPath),
                    FeatureSchemaVersion = TargetCandidateFeatures.FeatureSchemaVersion,
                    MlNetVersion = mlNetVersion
                };

                await bots.DartBotModelRepository.AddAsync(modelEntity);

                training.Status = DartBotModelTrainingStatusEnum.Completed;
                training.FinishedAt = DateTime.UtcNow;
                training.TrainingDataCount = trainCount;
                training.MetricsJson = JsonSerializer.Serialize(new
                {
                    metrics.RootMeanSquaredError,
                    metrics.MeanAbsoluteError,
                    metrics.RSquared,
                    TopChoiceHitRate = Math.Round(topChoice.HitRate, 4),
                    TopChoiceMeanRegret = Math.Round(topChoice.MeanRegret, 4),
                    TestFraction = hyperparameters.TestFraction,
                    DurationSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 1)
                });

                await unitOfWork.SaveChangesAsync();

                Console.WriteLine($"Modell gespeichert: {modelPath}");
                Console.WriteLine($"DartBotModels: Id {modelEntity.Id}, TargetSelection Version {versionNumber} (nicht aktiviert)");

                if (hooks is not null)
                {
                    hooks.CreatedId = modelEntity.Id;
                }
                return 0;
            }
            catch (Exception exception)
            {
                training.Status = DartBotModelTrainingStatusEnum.Failed;
                training.FinishedAt = DateTime.UtcNow;
                training.ErrorMessage = exception.GetBaseException().Message;
                await unitOfWork.SaveChangesAsync();

                Console.Error.WriteLine($"Training fehlgeschlagen: {training.ErrorMessage}");
                return 3;
            }
        }

        /// <summary>
        /// Was für die Zielwahl zählt: Wählt das Modell je Testsituation den Kandidaten mit dem kleinsten
        /// simulierten Wert? Verlust = simulierte Darts des gewählten Kandidaten minus die des besten.
        /// </summary>
        private static (double HitRate, double MeanRegret) TopChoiceAccuracy(MLContext ml, IDataView scored)
        {
            var rows = ml.Data.CreateEnumerable<ScoredRow>(scored, reuseRowObject: false).ToList();
            var situations = rows.GroupBy(r => r.SituationId).Where(g => g.Count() > 1).ToList();

            if (situations.Count == 0)
            {
                return (0, 0);
            }

            var hits = 0;
            double regret = 0;

            foreach (var situation in situations)
            {
                var chosen = situation.MinBy(r => r.Score)!;
                regret += chosen.RelativeDarts;
                hits += chosen.RelativeDarts <= 0.0001f ? 1 : 0;
            }

            return (hits / (double)situations.Count, regret / situations.Count);
        }

        private sealed class ScoredRow
        {
            public float SituationId { get; set; }
            public float RelativeDarts { get; set; }
            public float Score { get; set; }
        }

        private static long CountRows(MLContext ml, IDataView view) =>
            view.GetRowCount() ?? ml.Data.CreateEnumerable<TargetCandidateRow>(view, reuseRowObject: true).LongCount();

        private static async Task<int> NextVersionAsync(
            Shared.Interfaces.Repositories.IRepositoryBase<DartBotModelEntity> models, DartBotModelTypeEnum modelType)
        {
            var existing = await models.GetWhereAsync(m => m.ModelType == modelType, asNoTracking: true);
            return existing.Count == 0 ? 1 : existing.Max(m => m.VersionNumber) + 1;
        }
    }
}
