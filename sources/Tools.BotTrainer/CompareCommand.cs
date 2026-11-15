using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Logic.Bot.Ml;
using Logic.Bot.Models;
using Logic.Bot.Simulation;
using Logic.Bot.Tactics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML;
using Shared.Enums.X01;
using Shared.Interfaces.Bot;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Bot;
using Shared.Models.Bot.Ml;

namespace Tools.BotTrainer
{
    /// <summary>
    /// Phase 4: Modell gegen Regelwerk. Je Skill-Stufe werden dieselben Legs (gleicher Seed je Leg für die Würfe)
    /// einmal mit der Modell-Zielwahl und einmal mit RuleBasedTargetSelector gespielt; verglichen werden die Darts pro Leg.
    /// Gepaarter Vergleich: Differenz je Leg, Mittelwert und 95-%-Intervall. Ergebnis landet in MetricsJson des Trainingslaufs.
    /// </summary>
    public static class CompareCommand
    {
        /// <summary>Unter ~0,4 enden die meisten Legs an der Obergrenze; solche Stufen sagen nichts über die Zielwahl.</summary>
        private static readonly float[] SkillLevels = [0.5f, 0.7f, 0.85f, 0.95f];

        public static async Task<int> RunAsync(Arguments arguments, MlDataStore store, ServiceProvider provider)
        {
            var modelId = arguments.Int("model", 0);

            if (modelId == 0)
            {
                Console.Error.WriteLine("--model <Id> fehlt (Id aus DartBotModels).");
                return 1;
            }

            var legs = arguments.Int("legs", 2000);
            var seed = arguments.Int("seed", 1);
            var maxDarts = arguments.Int("max-darts", 60);
            var outMode = arguments.Flag("straight-out") ? X01OutModeEnum.Straight : X01OutModeEnum.Double;

            await using var scope = provider.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var modelEntity = await unitOfWork.DartBotUnitOfWork.DartBotModelRepository.GetFirstOrDefaultAsync(
                m => m.Id == modelId, include: q => q.Include(m => m.Training));

            if (modelEntity is null)
            {
                Console.Error.WriteLine($"Modell {modelId} nicht gefunden.");
                return 1;
            }

            if (modelEntity.FeatureSchemaVersion != TargetCandidateFeatures.FeatureSchemaVersion)
            {
                Console.Error.WriteLine($"Modell hat FeatureSchemaVersion {modelEntity.FeatureSchemaVersion}, erwartet {TargetCandidateFeatures.FeatureSchemaVersion}.");
                return 1;
            }

            var modelPath = store.PathOf(modelEntity.StorageKey, mustBeNew: false);

            if (!File.Exists(modelPath) ||
                !string.Equals(MlDataStore.Sha256Of(modelPath), modelEntity.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"Modelldatei fehlt oder wurde verändert: {modelPath}");
                return 1;
            }

            var ml = new MLContext(seed);
            var model = ml.Model.Load(modelPath, out _);

            Console.WriteLine($"Vergleich Modell {modelEntity.Id} (Version {modelEntity.VersionNumber}) gegen Regelwerk: " +
                              $"{legs} Legs je Stufe, 501 {(outMode == X01OutModeEnum.Straight ? "Straight-Out" : "Double-Out")}, Seed {seed}");
            Console.WriteLine();
            Console.WriteLine("  Stufe   Regelwerk   Modell   Differenz (95 %)        Modell besser / schlechter");

            var stopwatch = Stopwatch.StartNew();
            var levels = new List<object>();
            var anyWorse = false;

            foreach (var skill in SkillLevels)
            {
                var profile = new BotSkillProfile(skill, skill, 1f, skill, 0.5f);
                var situation = new BotSituation(501, 3, 501, profile, X01InModeEnum.Straight, outMode);

                var ruleDarts = new int[legs];
                var modelDarts = new int[legs];
                using var selectors = new ThreadLocal<MlTargetSelector>(() => new MlTargetSelector(ml, model));

                Parallel.For(0, legs, i =>
                {
                    var legSeed = unchecked(seed * 1_000_003 + i);
                    ruleDarts[i] = PlayLeg(situation, new RuleBasedTargetSelector(new DefaultRandomSource(new Random(~legSeed))), legSeed, maxDarts);
                    modelDarts[i] = PlayLeg(situation, selectors.Value!, legSeed, maxDarts);
                });

                var differences = modelDarts.Zip(ruleDarts, (m, r) => (double)(m - r)).ToArray();
                var meanDifference = differences.Average();
                var standardError = Math.Sqrt(differences.Sum(d => (d - meanDifference) * (d - meanDifference)) / (legs - 1) / legs);
                var low = meanDifference - 1.96 * standardError;
                var high = meanDifference + 1.96 * standardError;
                var better = differences.Count(d => d < 0);
                var worse = differences.Count(d => d > 0);

                // Signifikant schlechter = ganzes Intervall über 0.
                anyWorse |= low > 0;

                Console.WriteLine($"  {skill,5:0.00}   {ruleDarts.Average(),9:0.00}   {modelDarts.Average(),6:0.00}   {meanDifference,6:+0.00;-0.00} [{low:+0.00;-0.00}; {high:+0.00;-0.00}]   {better * 100.0 / legs,5:0.0} % / {worse * 100.0 / legs:0.0} %");

                levels.Add(new
                {
                    Skill = skill,
                    RuleDartsPerLeg = Math.Round(ruleDarts.Average(), 3),
                    ModelDartsPerLeg = Math.Round(modelDarts.Average(), 3),
                    MeanDifference = Math.Round(meanDifference, 3),
                    Ci95Low = Math.Round(low, 3),
                    Ci95High = Math.Round(high, 3),
                    ModelBetterShare = Math.Round(better / (double)legs, 4),
                    ModelWorseShare = Math.Round(worse / (double)legs, 4)
                });
            }

            Console.WriteLine();
            Console.WriteLine($"Dauer {stopwatch.Elapsed:hh\\:mm\\:ss}. Differenz < 0 = Modell braucht weniger Darts.");
            Console.WriteLine(anyWorse
                ? "Ergebnis: Modell ist auf mindestens einer Stufe signifikant schlechter – nicht aktivieren."
                : "Ergebnis: Modell ist auf keiner Stufe signifikant schlechter.");

            // Vergleich am Trainingslauf ablegen (das Modell selbst ist unveränderlich).
            var metrics = string.IsNullOrWhiteSpace(modelEntity.Training.MetricsJson)
                ? new JsonObject()
                : JsonNode.Parse(modelEntity.Training.MetricsJson) as JsonObject ?? new JsonObject();

            metrics["Comparison"] = JsonSerializer.SerializeToNode(new
            {
                ComparedAt = DateTime.UtcNow,
                Opponent = "RuleBasedTargetSelector",
                LegsPerLevel = legs,
                Seed = seed,
                OutMode = outMode.ToString(),
                MaxDartsPerLeg = maxDarts,
                SignificantlyWorse = anyWorse,
                Levels = levels
            });

            modelEntity.Training.MetricsJson = metrics.ToJsonString();
            await unitOfWork.SaveChangesAsync();

            Console.WriteLine($"Gespeichert in DartBotModelTrainings.Id {modelEntity.TrainingId} (MetricsJson.Comparison).");
            return anyWorse ? 4 : 0;
        }

        /// <summary>Ein Leg ab 501; die Würfe nutzen für beide Strategien denselben Zufallsstrom (gepaarter Vergleich).</summary>
        private static int PlayLeg(BotSituation situation, ITargetSelector selector, int legSeed, int maxDarts)
        {
            var simulator = new LegRolloutSimulator(selector, new ThrowSimulator(new DefaultRandomSource(new Random(legSeed))));

            var first = selector.SelectTarget(new BotThrowDecisionContext(
                situation.Remaining, situation.DartsAvailable, situation.OpponentRemaining, situation.Profile,
                situation.InMode, situation.OutMode, situation.Opened));

            return simulator.Play(situation, first, maxDarts);
        }
    }
}
