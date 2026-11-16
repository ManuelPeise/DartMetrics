using Logic.Bot.Simulation;
using Logic.Bot.Tactics;
using Logic.Shared.Rules;
using Shared.Enums.X01;
using Shared.Interfaces.Bot;
using Shared.Models.Bot;

namespace Logic.Bot.Play
{
    /// <summary>
    /// Ablauf je Dart: Ziel wählen -> werfen -> Treffer auflösen -> Bust/Checkout prüfen (X01Rules mit In-/Out-Modus).
    /// Double-In: vor der Eröffnung zählen Darts 0, der eröffnende Double zählt bereits.
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

        /// <summary>Zusätzliche Streuung auf Match-Darts bzw. im Entscheidungs-Leg bei Druckresistenz 0.</summary>
        public const float MaxMatchPressurePenalty = 0.3f;

        /// <summary>Match-Dart / Entscheidungs-Leg: mehr Streuung, je weniger Druckresistenz.</summary>
        public static BotSkillProfile WithMatchPressure(BotSkillProfile profile, float matchPressure)
        {
            if (matchPressure <= 0f)
            {
                return profile;
            }

            var factor = 1f + Math.Clamp(matchPressure, 0f, 1f) * MaxMatchPressurePenalty * (1f - Math.Clamp(profile.PressureResistance, 0f, 1f));
            return profile with { SigmaMm = (float)ThrowSimulator.HorizontalSigma(profile) * factor };
        }

        /// <summary>Lieblings- bzw. Angstdoppel: Streuung für dieses Ziel anpassen.</summary>
        public static BotSkillProfile WithFieldFactor(BotSkillProfile profile, DartTarget target)
        {
            var factor = profile.FieldFactor(target);
            return factor == 1f ? profile : profile with { SigmaMm = (float)ThrowSimulator.HorizontalSigma(profile) * factor };
        }

        public BotVisitResult PlayVisit(BotVisitContext context) => PlayVisit(context, _targetSelector);

        public BotVisitResult PlayVisit(BotVisitContext context, ITargetSelector targetSelector)
        {
            ArgumentNullException.ThrowIfNull(targetSelector);

            if (context.ScoreBefore < X01Rules.MinRemaining(context.OutMode))
            {
                throw new ArgumentException("Der Reststand ist zu klein für eine Aufnahme.", nameof(context));
            }

            var result = new BotVisitResult { ScoreBefore = context.ScoreBefore };
            var visitForm = _throwSimulator.DrawVisitForm(context.Profile);
            var remaining = context.ScoreBefore;
            var visitScore = 0;
            var isOpened = context.IsOpened || context.InMode == X01InModeEnum.Straight;
            var dartsInBoard = new List<BoardPoint>(DartsPerVisit);

            for (var throwNumber = 1; throwNumber <= DartsPerVisit; throwNumber++)
            {
                var dartsAvailable = DartsPerVisit - throwNumber + 1;

                var target = targetSelector.SelectTarget(new BotThrowDecisionContext(
                    remaining,
                    dartsAvailable,
                    context.OpponentScoreBefore,
                    context.Profile,
                    context.InMode,
                    context.OutMode,
                    isOpened,
                    context.ScoreBefore));

                var isFinishingDart = isOpened && target.Score == remaining &&
                                      _rules.IsFinishingDart(target.Segment, target.Multiplier, context.OutMode);
                var isPressure = isFinishingDart || context.OpponentScoreBefore <= OpponentPressureThreshold;

                var aimPoint = DartBoardGeometry.GetAimPoint(target);
                var profile = WithFieldFactor(isFinishingDart ? WithMatchPressure(context.Profile, context.MatchPressure) : context.Profile, target);
                var hitPoint = _throwSimulator.Throw(aimPoint, profile, visitForm, isPressure, dartsInBoard);
                dartsInBoard.Add(hitPoint);
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

                if (!isOpened && _rules.IsOpeningDart(hit.Segment, hit.Multiplier, context.InMode))
                {
                    isOpened = true;
                }

                // Vor der Eröffnung (Double-In) zählt der Dart nicht.
                var dartScore = isOpened ? hit.Score : 0;

                // X01Rules prüft gegen den Stand zu Beginn der Aufnahme.
                if (_rules.IsBust(context.ScoreBefore, visitScore + dartScore, hit.Segment, hit.Multiplier, context.OutMode))
                {
                    result.IsBust = true;
                    result.ScoreAfter = context.ScoreBefore;
                    return result;
                }

                if (_rules.IsCheckout(context.ScoreBefore, visitScore + dartScore, hit.Segment, hit.Multiplier, context.OutMode))
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
