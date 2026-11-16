using System.Collections.Concurrent;
using Logic.Bot.Simulation;
using Logic.Bot.Models;
using Logic.Shared.Rules;
using Shared.Enums.X01;
using Shared.Models.Bot;
using Shared.Models.Bot.Ml;

namespace Logic.Bot.Ml
{
    /// <summary>Baut die Merkmale eines Kandidaten (FeatureSchemaVersion 3) ohne Label.</summary>
    public sealed class CandidateFeatureBuilder
    {
        /// <summary>Würfe je Ziel und Profil-Stufe für Treffer-/Bust-Wahrscheinlichkeit.</summary>
        public const int ProbabilityThrows = 100;

        /// <summary>Profil-Werte werden für den Zwischenspeicher auf diese Schrittweite gerundet.</summary>
        private const float ProfileStep = 0.05f;

        /// <summary>Kürzester Checkout-Weg je Rest (GetCheckoutSuggestions ist teuer, daher Cache).</summary>
        private static readonly ConcurrentDictionary<(int Leftover, X01OutModeEnum OutMode), int> MinDartsCache = new();

        /// <summary>Simulierte Treffer je Ziel und gerundetem Profil (Präzision, Konstanz).</summary>
        private static readonly ConcurrentDictionary<(DartTarget Target, int Precision, int Consistency), DartTarget[]> ThrowCache = new();

        private readonly X01Rules _rules = new();

        public TargetCandidateFeatures Build(BotSituation situation, DartTarget candidate)
        {
            var leftover = CandidateGenerator.LeftoverIfHit(situation, candidate);
            var profile = situation.Profile;
            var isFinishing = leftover == 0 && _rules.IsFinishingDart(candidate.Segment, candidate.Multiplier, situation.OutMode);
            var minDarts = isFinishing ? 0 : leftover > 0 ? MinDarts(leftover, situation.OutMode) : 4;
            var (hit, bust) = Probabilities(situation, candidate);

            return new TargetCandidateFeatures
            {
                Remaining = situation.Remaining,
                DartsAvailable = situation.DartsAvailable,
                OpponentRemaining = situation.OpponentRemaining,
                OutMode = (float)situation.OutMode,
                IsOpened = situation.Opened,
                ThrowPrecision = profile.ThrowPrecision,
                ThrowConsistency = profile.ThrowConsistency,
                TacticalIntelligence = profile.TacticalIntelligence,
                PressureResistance = profile.PressureResistance,
                RiskTaking = profile.RiskTaking,
                CandidateSegment = candidate.Segment,
                CandidateMultiplier = candidate.Multiplier,
                CandidateScore = candidate.Score,
                LeftoverIfHit = leftover,
                IsFinishingDart = isFinishing,
                LeftoverIsCheckable = leftover > 0 && minDarts <= 3,
                LeftoverMinDarts = minDarts,
                LeftoverFinishableThisVisit = leftover > 0 && minDarts <= situation.DartsAvailable - 1,
                HitProbability = hit,
                BustProbability = bust
            };
        }

        private int MinDarts(int leftover, X01OutModeEnum outMode) =>
            MinDartsCache.GetOrAdd((leftover, outMode), key =>
            {
                if (key.Leftover > 180)
                {
                    return 4;
                }

                for (var darts = 1; darts <= 3; darts++)
                {
                    if (_rules.GetCheckoutSuggestions(key.Leftover, darts, key.OutMode).Count > 0)
                    {
                        return darts;
                    }
                }

                return 4;
            });

        /// <summary>
        /// Treffer- und Bust-Anteil aus simulierten Würfen (ohne Druck, mit Form-Streuung). Die Würfe hängen nur vom Ziel
        /// und vom gerundeten Profil ab und werden wiederverwendet; der Bust hängt vom aktuellen Rest ab.
        /// </summary>
        private (float Hit, float Bust) Probabilities(BotSituation situation, DartTarget candidate)
        {
            var precision = (int)MathF.Round(situation.Profile.ThrowPrecision / ProfileStep);
            var consistency = (int)MathF.Round(situation.Profile.ThrowConsistency / ProfileStep);

            var throws = ThrowCache.GetOrAdd((candidate, precision, consistency), key => SimulateThrows(key.Target, key.Precision, key.Consistency));

            var hits = 0;
            var busts = 0;

            foreach (var landed in throws)
            {
                if (landed == candidate)
                {
                    hits++;
                }

                if (situation.Opened && _rules.IsBust(situation.Remaining, landed.Score, landed.Segment, landed.Multiplier, situation.OutMode))
                {
                    busts++;
                }
            }

            return (hits / (float)throws.Length, busts / (float)throws.Length);
        }

        private static DartTarget[] SimulateThrows(DartTarget target, int precisionStep, int consistencyStep)
        {
            var profile = new BotSkillProfile(precisionStep * ProfileStep, consistencyStep * ProfileStep, 1f, 1f, 0.5f);

            // Fester Seed je Schlüssel: gleiche Merkmale in Training und Spiel (HashCode.Combine wäre je Prozess anders).
            var seed = ((target.Segment * 4 + target.Multiplier) * 32 + precisionStep) * 32 + consistencyStep + 1;
            var simulator = new ThrowSimulator(new DefaultRandomSource(new Random(seed)));
            var aim = DartBoardGeometry.GetAimPoint(target);
            var result = new DartTarget[ProbabilityThrows];

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = DartBoardGeometry.Resolve(simulator.Throw(aim, profile, simulator.DrawVisitForm(profile), isPressureSituation: false));
            }

            return result;
        }
    }
}
