using Logic.Bot.Models;
using Logic.Bot.Simulation;
using Shared.Models.Bot;

namespace Logic.Bot.Policy
{
    /// <summary>
    /// Trefferverteilung je Ziel für ein Profil: wohin landet der Dart, wenn auf X gezielt wird (mit und ohne Druck).
    /// Geschätzt aus dem Wurf-Simulator; die Tagesform wird je Wurf neu gezogen (Randverteilung).
    /// Für die Rechnung zählt nur der Wert des Treffers und ob er ein Doppel ist (Bull zählt als Doppel).
    /// </summary>
    public sealed class ThrowOutcomeModel
    {
        /// <summary>Alle Ziele: S/D/T 1–20, 25, Bull.</summary>
        public static readonly DartTarget[] Targets = BuildTargets();

        /// <summary>Seltener als das werden Treffer zum Miss gezählt (spart Rechenzeit, Wirkung vernachlässigbar).</summary>
        private const float MinProbability = 0.0005f;

        private readonly Outcome[][] _normal;
        private readonly Outcome[][] _pressure;

        private ThrowOutcomeModel(BotSkillProfile profile, Outcome[][] normal, Outcome[][] pressure)
        {
            Profile = profile;
            _normal = normal;
            _pressure = pressure;
        }

        public BotSkillProfile Profile { get; }

        public readonly record struct Outcome(int Score, bool IsDouble, float Probability);

        public static int IndexOf(DartTarget target) => Array.IndexOf(Targets, target);

        public Outcome[] Outcomes(int targetIndex, bool pressure) => pressure ? _pressure[targetIndex] : _normal[targetIndex];

        public static ThrowOutcomeModel Build(BotSkillProfile profile, int samplesPerTarget = 4000, int seed = 1)
        {
            var simulator = new ThrowSimulator(new DefaultRandomSource(new Random(seed)));
            var normal = new Outcome[Targets.Length][];
            var pressure = new Outcome[Targets.Length][];

            for (var i = 0; i < Targets.Length; i++)
            {
                var aim = DartBoardGeometry.GetAimPoint(Targets[i]);
                normal[i] = Sample(simulator, profile, aim, isPressure: false, samplesPerTarget);
                pressure[i] = Sample(simulator, profile, aim, isPressure: true, samplesPerTarget);
            }

            return new ThrowOutcomeModel(profile, normal, pressure);
        }

        /// <summary>Für Tests: Verteilung direkt vorgeben (gleich mit und ohne Druck).</summary>
        public static ThrowOutcomeModel FromDistribution(BotSkillProfile profile, Func<DartTarget, IEnumerable<Outcome>> distribution)
        {
            var outcomes = Targets.Select(t => distribution(t).ToArray()).ToArray();
            return new ThrowOutcomeModel(profile, outcomes, outcomes);
        }

        private static Outcome[] Sample(ThrowSimulator simulator, BotSkillProfile profile, BoardPoint aim, bool isPressure, int samples)
        {
            var counts = new Dictionary<(int Score, bool IsDouble), int>();

            for (var n = 0; n < samples; n++)
            {
                var hit = DartBoardGeometry.Resolve(simulator.Throw(aim, profile, simulator.DrawVisitForm(profile), isPressure));
                var key = (hit.Score, hit.Multiplier == 2);
                counts[key] = counts.GetValueOrDefault(key) + 1;
            }

            var result = new List<Outcome>();
            var missProbability = 0f;

            foreach (var ((score, isDouble), count) in counts)
            {
                var probability = count / (float)samples;

                if (score == 0 || probability < MinProbability)
                {
                    missProbability += probability;
                    continue;
                }

                result.Add(new Outcome(score, isDouble, probability));
            }

            if (missProbability > 0)
            {
                result.Add(new Outcome(0, false, missProbability));
            }

            return [.. result.OrderByDescending(o => o.Probability)];
        }

        private static DartTarget[] BuildTargets()
        {
            var targets = new List<DartTarget>();

            for (var segment = 1; segment <= 20; segment++)
            {
                targets.Add(DartTarget.Single(segment));
                targets.Add(DartTarget.Double(segment));
                targets.Add(DartTarget.Triple(segment));
            }

            targets.Add(DartTarget.Single(25));
            targets.Add(DartTarget.Double(25));

            return [.. targets];
        }
    }
}
