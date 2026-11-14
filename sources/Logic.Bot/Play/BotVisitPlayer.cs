using Logic.Bot.Simulation;
using Logic.Bot.Tactics;
using Logic.Shared.Rules;
using Shared.Interfaces.Bot;
using Shared.Models.Bot;

namespace Logic.Bot.Play
{
    /// <summary>
    /// Ablauf je Dart: Ziel wählen -> werfen -> Treffer auflösen -> Bust/Checkout prüfen (X01Rules, Double-Out).
    /// Nach Bust oder Checkout endet die Aufnahme sofort.
    /// </summary>
    public sealed class BotVisitPlayer : IBotVisitPlayer
    {
        private const int DartsPerVisit = 3;

        /// <summary>Gegner ab diesem Rest gilt als "auf dem Finish" -> Drucksituation.</summary>
        private const int OpponentPressureThreshold = 100;

        private readonly X01Rules _rules = new();
        private readonly ITargetSelector _targetSelector;
        private readonly IThrowSimulator _throwSimulator;

        public BotVisitPlayer(ITargetSelector targetSelector, IThrowSimulator throwSimulator)
        {
            _targetSelector = targetSelector;
            _throwSimulator = throwSimulator;
        }

        public BotVisitResult PlayVisit(BotVisitContext context)
        {
            if (context.ScoreBefore < 2)
            {
                throw new ArgumentException("Der Reststand muss mindestens 2 sein.", nameof(context));
            }

            var result = new BotVisitResult { ScoreBefore = context.ScoreBefore };
            var visitForm = _throwSimulator.DrawVisitForm(context.Profile);
            var remaining = context.ScoreBefore;
            var visitScore = 0;

            for (var throwNumber = 1; throwNumber <= DartsPerVisit; throwNumber++)
            {
                var dartsAvailable = DartsPerVisit - throwNumber + 1;

                var target = _targetSelector.SelectTarget(new BotThrowDecisionContext(
                    remaining,
                    dartsAvailable,
                    context.OpponentScoreBefore,
                    context.Profile));

                var isFinishingDart = target.IsDouble && target.Score == remaining;
                var isPressure = isFinishingDart || context.OpponentScoreBefore <= OpponentPressureThreshold;

                var aimPoint = DartBoardGeometry.GetAimPoint(target);
                var hitPoint = _throwSimulator.Throw(aimPoint, context.Profile, visitForm, isPressure);
                var hit = DartBoardGeometry.Resolve(hitPoint);

                result.Throws.Add(new BotThrowResult
                {
                    ThrowNumber = throwNumber,
                    TargetSegment = target.Segment,
                    TargetMultiplier = target.Multiplier,
                    TargetX = (float)aimPoint.X,
                    TargetY = (float)aimPoint.Y,
                    Segment = hit.Segment,
                    Multiplier = hit.Multiplier,
                    Score = _rules.CalculateScore(hit.Segment, hit.Multiplier),
                    HitX = (float)hitPoint.X,
                    HitY = (float)hitPoint.Y,
                    ScoreBeforeThrow = remaining,
                    DartsLeftInVisit = DartsPerVisit - throwNumber
                });

                var dartScore = hit.Score;

                // X01Rules prüft gegen den Stand zu Beginn der Aufnahme.
                if (_rules.IsBust(context.ScoreBefore, visitScore + dartScore, hit.Segment, hit.Multiplier))
                {
                    result.IsBust = true;
                    result.ScoreAfter = context.ScoreBefore;
                    return result;
                }

                if (_rules.IsCheckout(context.ScoreBefore, visitScore + dartScore, hit.Segment, hit.Multiplier))
                {
                    result.IsCheckout = true;
                    result.ScoreAfter = 0;
                    return result;
                }

                visitScore += dartScore;
                remaining -= dartScore;
            }

            result.ScoreAfter = remaining;
            return result;
        }
    }
}
