using System.Collections.Concurrent;
using Logic.Shared.Rules;
using Shared.Enums.X01;
using Shared.Models.Bot;
using Shared.Models.Bot.Ml;

namespace Logic.Bot.Ml
{
    /// <summary>Baut die Merkmale eines Kandidaten (FeatureSchemaVersion 1) ohne Label.</summary>
    public sealed class CandidateFeatureBuilder
    {
        /// <summary>Ist ein Rest in einer Aufnahme (3 Darts) checkbar? GetCheckoutSuggestions ist teuer, daher Cache.</summary>
        private static readonly ConcurrentDictionary<(int Leftover, X01OutModeEnum OutMode), bool> CheckableCache = new();

        private readonly X01Rules _rules = new();

        public TargetCandidateFeatures Build(BotSituation situation, DartTarget candidate)
        {
            var leftover = CandidateGenerator.LeftoverIfHit(situation, candidate);
            var profile = situation.Profile;

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
                IsFinishingDart = leftover == 0 && _rules.IsFinishingDart(candidate.Segment, candidate.Multiplier, situation.OutMode),
                LeftoverIsCheckable = leftover > 0 && IsCheckable(leftover, situation.OutMode)
            };
        }

        private bool IsCheckable(int leftover, X01OutModeEnum outMode) =>
            CheckableCache.GetOrAdd((leftover, outMode), key =>
                key.Leftover <= 180 && _rules.GetCheckoutSuggestions(key.Leftover, 3, key.OutMode).Count > 0);
    }
}
