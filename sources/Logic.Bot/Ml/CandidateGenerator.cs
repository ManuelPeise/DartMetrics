using Logic.Shared.Rules;
using Shared.Enums.X01;
using Shared.Models.Bot;
using Shared.Models.Bot.Ml;

namespace Logic.Bot.Ml
{
    /// <summary>
    /// Alle sinnvollen Ziele für den nächsten Dart: S/D/T 1–20, 25, Bull.
    /// Ausgeschlossen sind Ziele, die bei Treffer sicher busten (Rest unter X01Rules.MinRemaining,
    /// unter 0, oder 0 ohne gültigen Abschluss). Bei Double-In vor der Eröffnung nur Doubles und Bull.
    /// </summary>
    public sealed class CandidateGenerator
    {
        private readonly X01Rules _rules = new();

        public IReadOnlyList<DartTarget> Generate(BotSituation situation)
        {
            var candidates = new List<DartTarget>(62);

            foreach (var target in AllTargets())
            {
                if (!situation.Opened)
                {
                    if (!target.IsDouble)
                    {
                        continue;
                    }
                }

                if (IsSureBust(situation, target))
                {
                    continue;
                }

                candidates.Add(target);
            }

            return candidates;
        }

        /// <summary>Rest nach einem Treffer auf dieses Ziel (vor der Eröffnung zählt nur ein eröffnendes Double).</summary>
        public static int LeftoverIfHit(BotSituation situation, DartTarget target)
        {
            var counts = situation.Opened || target.IsDouble;
            return situation.Remaining - (counts ? target.Score : 0);
        }

        private bool IsSureBust(BotSituation situation, DartTarget target)
        {
            var leftover = LeftoverIfHit(situation, target);

            if (leftover < 0)
            {
                return true;
            }

            if (leftover == 0)
            {
                return !_rules.IsFinishingDart(target.Segment, target.Multiplier, situation.OutMode);
            }

            return leftover < X01Rules.MinRemaining(situation.OutMode);
        }

        private static IEnumerable<DartTarget> AllTargets()
        {
            for (var segment = 20; segment >= 1; segment--)
            {
                yield return DartTarget.Triple(segment);
                yield return DartTarget.Double(segment);
                yield return DartTarget.Single(segment);
            }

            yield return DartTarget.Double(25);
            yield return DartTarget.Single(25);
        }
    }
}
