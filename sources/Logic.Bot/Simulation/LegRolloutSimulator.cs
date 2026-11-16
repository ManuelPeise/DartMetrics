using Logic.Bot.Simulation;
using Logic.Shared.Rules;
using Shared.Interfaces.Bot;
using Shared.Models.Bot;

namespace Logic.Bot.Simulation
{
    /// <summary>
    /// Spielt ab einem vorgegebenen ersten Ziel das Leg zu Ende und zählt die Darts (Kandidaten-Dart inklusive).
    /// Ablauf je Dart wie BotVisitPlayer: Ziel wählen (ab dem zweiten Dart über die Strategie) → werfen → auflösen →
    /// Bust/Checkout über X01Rules. Der Gegner wirft nicht; sein Stand bleibt als Merkmal fest.
    /// </summary>
    public sealed class LegRolloutSimulator
    {
        private const int DartsPerVisit = 3;

        /// <summary>Wie BotVisitPlayer: Gegner auf dem Finish → Drucksituation.</summary>
        private const int OpponentPressureThreshold = 100;

        private readonly X01Rules _rules = new();
        private readonly ITargetSelector _strategy;
        private readonly IThrowSimulator _throwSimulator;

        public LegRolloutSimulator(ITargetSelector strategy, IThrowSimulator throwSimulator)
        {
            _strategy = strategy;
            _throwSimulator = throwSimulator;
        }

        public int Play(BotSituation situation, DartTarget firstTarget, int maxDarts)
        {
            var profile = situation.Profile;
            var remaining = situation.Remaining;
            var visitStart = situation.VisitStart;
            var dartsLeft = situation.DartsAvailable;
            var opened = situation.Opened;
            // Mitten in einer Aufnahme ist unbekannt, ob die Eröffnung erst in dieser Aufnahme fiel; angenommen wird „vorher“.
            var openedAtVisitStart = opened;
            var visitScore = visitStart - remaining;
            var form = _throwSimulator.DrawVisitForm(profile);
            DartTarget? forced = firstTarget;
            var darts = 0;

            while (darts < maxDarts)
            {
                var target = forced ?? _strategy.SelectTarget(new BotThrowDecisionContext(
                    remaining, dartsLeft, situation.OpponentRemaining, profile, situation.InMode, situation.OutMode, opened, visitStart));
                forced = null;

                var isFinishingDart = opened && target.Score == remaining &&
                                      _rules.IsFinishingDart(target.Segment, target.Multiplier, situation.OutMode);
                var isPressure = isFinishingDart || situation.OpponentRemaining <= OpponentPressureThreshold;

                var hitPoint = _throwSimulator.Throw(DartBoardGeometry.GetAimPoint(target), profile, form, isPressure);
                var hit = DartBoardGeometry.Resolve(hitPoint);
                darts++;

                if (!opened && _rules.IsOpeningDart(hit.Segment, hit.Multiplier, situation.InMode))
                {
                    opened = true;
                }

                var dartScore = opened ? hit.Score : 0;

                if (_rules.IsCheckout(visitStart, visitScore + dartScore, hit.Segment, hit.Multiplier, situation.OutMode))
                {
                    return darts;
                }

                if (_rules.IsBust(visitStart, visitScore + dartScore, hit.Segment, hit.Multiplier, situation.OutMode))
                {
                    // Bust: Stand und Eröffnung fallen auf den Beginn der Aufnahme zurück, die Aufnahme endet.
                    remaining = visitStart;
                    opened = openedAtVisitStart;
                    dartsLeft = 0;
                }
                else
                {
                    visitScore += dartScore;
                    remaining -= dartScore;
                    dartsLeft--;
                }

                if (dartsLeft == 0)
                {
                    visitStart = remaining;
                    visitScore = 0;
                    openedAtVisitStart = opened;
                    dartsLeft = DartsPerVisit;
                    form = _throwSimulator.DrawVisitForm(profile);
                }
            }

            return maxDarts;
        }
    }
}
