using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Logic.Bot.Ml;
using Logic.Bot.Models;
using Logic.Bot.Simulation;
using Logic.Bot.Tactics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        public static async Task<int> RunAsync(Arguments arguments, MlDataStore store, ServiceProvider provider, JobHooks? hooks = null)
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
            var margins = arguments.Floats("margins", [0f, 0.3f, 0.5f, 1f]);
            var tactical = Math.Clamp(arguments.Float("tactical", 1f), 0f, 1f);

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

            var loaded = LoadedTargetModel.Load(modelPath, modelEntity.Sha256);

            Console.WriteLine($"Vergleich Modell {modelEntity.Id} (Version {modelEntity.VersionNumber}) gegen Regelwerk: " +
                              $"{legs} Legs je Stufe, 501 {(outMode == X01OutModeEnum.Straight ? "Straight-Out" : "Double-Out")}, " +
                              $"Taktik {tactical:0.##}, Schwellen {string.Join(" / ", margins.Select(m => m.ToString("0.##")))}, Seed {seed}");
            Console.WriteLine("Schwelle = so viele Darts muss das Modell besser sein, damit es das Regel-Ziel ersetzt (0 = reines Modell).");
            Console.WriteLine();
            Console.WriteLine("  Stufe  Schwelle   Regelwerk   Modell   Differenz (95 %)        besser / schlechter   abgewichen");

            var stopwatch = Stopwatch.StartNew();
            var levels = new List<object>();
            var worseByMargin = margins.ToDictionary(m => m, _ => false);

            var levelIndex = 0;

            foreach (var skill in SkillLevels)
            {
                if (hooks is not null)
                {
                    await hooks.ReportOrCancelAsync($"Vergleich: Skill-Stufe {levelIndex + 1}/{SkillLevels.Length}", levelIndex * 100 / SkillLevels.Length);
                }

                levelIndex++;
                var profile = new BotSkillProfile(skill, skill, tactical, skill, 0.5f);
                var situation = new BotSituation(501, 3, 501, profile, X01InModeEnum.Straight, outMode);

                var ruleDarts = new int[legs];
                Parallel.For(0, legs, i =>
                {
                    var legSeed = LegSeed(seed, i);
                    ruleDarts[i] = PlayLeg(situation, Rules(legSeed), legSeed, maxDarts);
                });

                foreach (var margin in margins)
                {
                    var modelDarts = new int[legs];
                    long decisions = 0, overrides = 0;

                    Parallel.For(0, legs, i =>
                    {
                        var legSeed = LegSeed(seed, i);
                        // Gleiche Würfe und gleicher Taktik-Zufall wie im Regelwerk-Leg: nur die Abweichungen des Modells zählen.
                        var selector = new HybridTargetSelector(loaded, Rules(legSeed), margin);
                        modelDarts[i] = PlayLeg(situation, selector, legSeed, maxDarts);
                        Interlocked.Add(ref decisions, selector.Decisions);
                        Interlocked.Add(ref overrides, selector.Overrides);
                    });

                    var differences = modelDarts.Zip(ruleDarts, (m, r) => (double)(m - r)).ToArray();
                    var meanDifference = differences.Average();
                    var standardError = Math.Sqrt(differences.Sum(d => (d - meanDifference) * (d - meanDifference)) / (legs - 1) / legs);
                    var low = meanDifference - 1.96 * standardError;
                    var high = meanDifference + 1.96 * standardError;
                    var better = differences.Count(d => d < 0);
                    var worse = differences.Count(d => d > 0);
                    var overrideShare = decisions == 0 ? 0 : overrides / (double)decisions;

                    // Signifikant schlechter = ganzes Intervall über 0.
                    worseByMargin[margin] |= low > 0;

                    Console.WriteLine($"  {skill,5:0.00}  {margin,8:0.0#}   {ruleDarts.Average(),9:0.00}   {modelDarts.Average(),6:0.00}   {meanDifference,6:+0.00;-0.00} [{low:+0.00;-0.00}; {high:+0.00;-0.00}]   {better * 100.0 / legs,5:0.0} % / {worse * 100.0 / legs,4:0.0} %   {overrideShare,8:P1}");

                    levels.Add(new
                    {
                        Skill = skill,
                        Margin = margin,
                        RuleDartsPerLeg = Math.Round(ruleDarts.Average(), 3),
                        ModelDartsPerLeg = Math.Round(modelDarts.Average(), 3),
                        MeanDifference = Math.Round(meanDifference, 3),
                        Ci95Low = Math.Round(low, 3),
                        Ci95High = Math.Round(high, 3),
                        ModelBetterShare = Math.Round(better / (double)legs, 4),
                        ModelWorseShare = Math.Round(worse / (double)legs, 4),
                        OverrideShare = Math.Round(overrideShare, 4)
                    });
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Dauer {stopwatch.Elapsed:hh\\:mm\\:ss}. Differenz < 0 = Modell braucht weniger Darts.");
            var safeMargins = margins.Where(m => !worseByMargin[m]).ToList();
            Console.WriteLine(safeMargins.Count == 0
                ? "Ergebnis: mit keiner Schwelle ist das Modell auf allen Stufen mindestens gleich gut – nicht aktivieren."
                : $"Ergebnis: auf keiner Stufe signifikant schlechter mit Schwelle {string.Join(", ", safeMargins.Select(m => m.ToString("0.##")))}.");

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
                TacticalIntelligence = tactical,
                Margins = margins,
                SafeMargins = safeMargins,
                Levels = levels
            });

            modelEntity.Training.MetricsJson = metrics.ToJsonString();
            await unitOfWork.SaveChangesAsync();

            Console.WriteLine($"Gespeichert in DartBotModelTrainings.Id {modelEntity.TrainingId} (MetricsJson.Comparison).");
            if (hooks is not null)
            {
                hooks.ComparisonPassed = safeMargins.Count > 0;
            }

            return safeMargins.Count == 0 ? 4 : 0;
        }

        private static int LegSeed(int seed, int leg) => unchecked(seed * 1_000_003 + leg);

        /// <summary>Regelwerk mit eigenem Zufall für die Taktik (getrennt von den Würfen), je Leg gleich.</summary>
        private static RuleBasedTargetSelector Rules(int legSeed) => new(new DefaultRandomSource(new Random(~legSeed)));

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
