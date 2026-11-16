using Shared.Enums.X01;
using Shared.Interfaces.Bot;
using Shared.Models.Bot;

namespace Logic.Bot.Policy
{
    /// <summary>
    /// Zielwahl aus der exakten Tabelle, „wie ein Profi“: nach jedem Dart der beste Wurf für den neuen Zustand.
    /// - Bekannte Wege: liegt das Ziel des Regelwerks (Standard-Checkout) höchstens <c>routeTolerance</c> Darts hinter dem
    ///   besten, wird es genommen – Bots spielen Standardwege statt exotischer, kaum besserer Varianten.
    /// - Taktik-Fehler: mit einer Wahrscheinlichkeit, die mit TacticalIntelligence sinkt, das zweitbeste Ziel.
    /// - Nicht abgedeckt (Double-In vor der Eröffnung, Zustand außerhalb der Tabelle): Regelwerk.
    /// </summary>
    public sealed class PolicyTableTargetSelector : ITargetSelector
    {
        /// <summary>Höchstwahrscheinlichkeit eines Taktik-Fehlers bei TacticalIntelligence 0 (wie im Regelwerk).</summary>
        public const double MaxTacticalMistakeChance = 0.3;

        /// <summary>Gegner auf dem Finish → Drucksituation (wie BotVisitPlayer).</summary>
        private const int OpponentPressureThreshold = 100;

        private readonly PolicyTableCache _cache;
        private readonly ITargetSelector _rules;
        private readonly IRandomSource _random;
        private readonly float _routeTolerance;

        public PolicyTableTargetSelector(PolicyTableCache cache, ITargetSelector rules, IRandomSource random, float routeTolerance = 0.05f)
        {
            _cache = cache;
            _rules = rules;
            _random = random;
            _routeTolerance = Math.Max(0f, routeTolerance);
        }

        public int Decisions { get; private set; }

        /// <summary>Entscheidungen, bei denen die Tabelle ein anderes Ziel wählt als das Regelwerk.</summary>
        public int Overrides { get; private set; }

        public DartTarget SelectTarget(BotThrowDecisionContext context)
        {
            if (!context.IsOpened && context.InMode == X01InModeEnum.Double)
            {
                return _rules.SelectTarget(context);
            }

            var table = _cache.Get(context.Profile, context.OutMode, context.OpponentRemaining <= OpponentPressureThreshold);
            var visitStart = context.ScoreAtVisitStart ?? context.Remaining;

            var dartsLeft = context.DartsAvailable;

            // Unbekannter oder unpassender Aufnahme-Beginn: so rechnen, als begänne die Aufnahme jetzt.
            if (!table.Covers(visitStart, context.Remaining, dartsLeft))
            {
                visitStart = context.Remaining;
            }

            if (!table.Covers(visitStart, context.Remaining, dartsLeft))
            {
                return _rules.SelectTarget(context);
            }

            Decisions++;

            // Ohne Taktik-Fehler: das Regelwerk liefert den Standardweg (TacticalIntelligence 1 = immer bester Regel-Weg).
            var standard = _rules.SelectTarget(context with { Profile = context.Profile with { TacticalIntelligence = 1f } });

            var bestIndex = -1;
            var secondIndex = -1;
            var bestQ = float.MaxValue;
            var secondQ = float.MaxValue;
            var standardQ = float.MaxValue;

            for (var t = 0; t < ThrowOutcomeModel.Targets.Length; t++)
            {
                var q = table.Q(visitStart, context.Remaining, dartsLeft, t);

                if (ThrowOutcomeModel.Targets[t] == standard)
                {
                    standardQ = q;
                }

                if (q < bestQ)
                {
                    secondQ = bestQ;
                    secondIndex = bestIndex;
                    bestQ = q;
                    bestIndex = t;
                }
                else if (q < secondQ)
                {
                    secondQ = q;
                    secondIndex = t;
                }
            }

            var choice = standardQ <= bestQ + _routeTolerance ? standard : ThrowOutcomeModel.Targets[bestIndex];

            var mistakeChance = (1.0 - Math.Clamp(context.Profile.TacticalIntelligence, 0f, 1f)) * MaxTacticalMistakeChance;

            // Fehler: ein anderes der beiden besten Ziele (wie ein Verrechnen unter Zeitdruck).
            if (secondIndex >= 0 && _random.NextDouble() < mistakeChance)
            {
                var second = ThrowOutcomeModel.Targets[secondIndex];
                choice = second != choice ? second : ThrowOutcomeModel.Targets[bestIndex];
            }

            if (choice != standard)
            {
                Overrides++;
            }

            return choice;
        }
    }
}
