using Data.DartContext.Entities.Training;
using Logic.Shared.Rules;
using Shared.Enums.Training;
using Shared.Enums.X01;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.Training;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Shared;
using Shared.Models.Training;

namespace Logic.Training
{
    public class X01TrainingGame : TrainingBase, IX01TrainingGame
    {
        private const int ThreeZeroOneScore = 301;
        private const int FiveZeroOneScore = 501;
        private readonly X01Rules _rules = new();

        public X01TrainingGame(
            IUnitOfWork unitOfWork,
            IMessageLog messageLog,
            IClaimsHelper claimsHelper) : base(unitOfWork, messageLog, claimsHelper)
        {
        }

        public async Task<RunningX01Training?> GetRunningTrainingAsync()
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var sessionId = await FindRunningSessionIdAsync(currentUser.UserId, TrainingTypeEnum.X01Training);

                if (sessionId is null)
                {
                    return null;
                }

                var training = await LoadX01Training(sessionId.Value, currentUser.UserId);
                var visits = training.Visits.OrderBy(v => v.VisitNumber).ToList();
                var last = visits.LastOrDefault();
                var lastThrows = last?.Throws.OrderBy(t => t.ThrowNumber).ToList() ?? new List<TrainingGameThrowEntity>();
                var isVisitOpen = last is not null && !last.IsBust && last.ScoreAfter != 0 && lastThrows.Count < 3;
                var lastLegFinished = last is not null && IsLegCheckout(last);
                var remaining = last is null || lastLegFinished ? training.StartingScore : last.ScoreAfter;
                var legNumber = last is null ? 1 : lastLegFinished ? last.LegNumber + 1 : last.LegNumber;

                // Gleiche Regel wie AddThrowAsync: eröffnet, sobald gepunktet wurde.
                var isOpened = training.InMode == X01InModeEnum.Straight || remaining < training.StartingScore;

                return new RunningX01Training
                {
                    TrainingId = training.TrainingSessionId,
                    StartedAt = training.TrainingSession.StartedAt,
                    StartingScore = training.StartingScore,
                    InMode = training.InMode,
                    OutMode = training.OutMode,
                    NextVisitNumber = isVisitOpen ? last!.VisitNumber : (last?.VisitNumber ?? 0) + 1,
                    NextThrowNumber = isVisitOpen ? lastThrows.Count + 1 : 1,
                    DartsThrown = visits.Sum(v => v.Throws.Count),
                    CurrentVisitDarts = ToDarts(lastThrows.Where(_ => isVisitOpen).Select(t => (t.Segment, t.Multiplier))),
                    RemainingScore = remaining,
                    LegNumber = legNumber,
                    LegsCompleted = visits.Count(IsLegCheckout),
                    NumberOfLegs = training.NumberOfLegs,
                    IsOpened = isOpened,
                    Busts = visits.Count(v => v.IsBust),
                    CheckoutSuggestions = isVisitOpen && isOpened
                        ? _rules.GetCheckoutSuggestions(remaining, 3 - lastThrows.Count, training.OutMode)
                        : new List<CheckoutSuggestion>()
                };
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error loading running X01 training.", nameof(X01TrainingGame), exception);
                throw;
            }
        }

        public async Task<int> StartTrainingAsync(StartX01TrainingModel model)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                if (model.StartingScore is not (ThreeZeroOneScore or FiveZeroOneScore))
                {
                    throw new Exception($"Starting score must be {ThreeZeroOneScore} or {FiveZeroOneScore}.");
                }

                if (!Enum.IsDefined(model.InMode) || !Enum.IsDefined(model.OutMode))
                {
                    throw new ArgumentException("Invalid in or out mode.");
                }

                var trainingSession = new TrainingSessionEntity
                {
                    TrainingType = TrainingTypeEnum.X01Training,
                    StartedAt = DateTime.UtcNow,
                    UserId = currentUser.UserId,
                    TrainingGame = new TrainingGameEntity
                    {
                        StartingScore = model.StartingScore,
                        InMode = model.InMode,
                        OutMode = model.OutMode,
                        // Training allein: immer ein Satz Best of 5, alle Legs werden gespielt.
                        NumberOfLegs = X01SetRules.TrainingBestOfLegs
                    }
                };

                await UnitOfWork.DartTrainingUnitOfWork.TrainingSessionRepository.AddAsync(trainingSession);
                await UnitOfWork.SaveChangesAsync();

                return trainingSession.Id;
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("", nameof(X01TrainingGame), exception);

                throw;
            }
        }

        public async Task<X01TrainingThrowResult> AddThrowAsync(X01TrainingThrow model)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var training = await LoadX01Training(model.TrainingId, currentUser.UserId);

                if (training.TrainingSession.FinishedAt.HasValue)
                {
                    throw new Exception("Training is already finished.");
                }

                if (model.VisitNumber < 1)
                {
                    throw new Exception("Invalid visit number.");
                }

                if (model.ThrowNumber is < 1 or > 3)
                {
                    throw new Exception("Invalid throw number.");
                }

                var visit = training.Visits
                    .FirstOrDefault(v => v.VisitNumber == model.VisitNumber);

                if (visit == null)
                {
                    var lastVisit = training.Visits
                        .OrderByDescending(v => v.VisitNumber)
                        .FirstOrDefault();

                    if (model.VisitNumber != (lastVisit?.VisitNumber ?? 0) + 1 ||
                        model.ThrowNumber != 1)
                        throw new Exception("Invalid visit or throw sequence.");

                    // Nach einem Checkout beginnt das nächste Leg wieder beim Startwert.
                    var lastLegFinished = lastVisit is not null && IsLegCheckout(lastVisit);
                    var scoreBefore = lastVisit is null || lastLegFinished ? training.StartingScore : lastVisit.ScoreAfter;
                    var legNumber = lastVisit is null ? 1 : lastLegFinished ? lastVisit.LegNumber + 1 : lastVisit.LegNumber;

                    visit = new TrainingGameVisitEntity
                    {
                        VisitNumber = model.VisitNumber,
                        LegNumber = legNumber,
                        ScoreBefore = scoreBefore,
                        ScoreAfter = scoreBefore
                    };

                    training.Visits.Add(visit);
                }

                if (visit.IsBust || visit.ScoreAfter == 0)
                {
                    throw new Exception("This visit is already completed.");
                }

                if (model.ThrowNumber != visit.Throws.Count + 1)
                {
                    throw new Exception("Throws must be added in sequence.");
                }

                if (!_rules.IsValidSegment(model.Segment) ||
                    !_rules.IsValidMultiplier(model.Segment, model.Multiplier))
                {
                    throw new Exception("Invalid dart throw.");
                }

                var score = _rules.CalculateScore(model.Segment, model.Multiplier);

                visit.Throws.Add(new TrainingGameThrowEntity
                {
                    ThrowNumber = model.ThrowNumber,
                    Segment = model.Segment,
                    Multiplier = model.Multiplier,
                    Score = score
                });

                // Double-In: eröffnet ist das Leg, sobald eine frühere Aufnahme gepunktet hat oder in dieser
                // Aufnahme ein Double getroffen wurde. Vorher zählen Darts 0 (Score bleibt der Trefferwert).
                var isOpenedBeforeVisit = training.InMode == X01InModeEnum.Straight || visit.ScoreBefore < training.StartingScore;

                var visitScore = _rules.CountedScore(
                    visit.Throws.OrderBy(t => t.ThrowNumber).Select(t => (t.Segment, t.Multiplier)),
                    isOpenedBeforeVisit,
                    training.InMode);

                var isOpened = isOpenedBeforeVisit ||
                               visit.Throws.Any(t => _rules.IsOpeningDart(t.Segment, t.Multiplier, training.InMode));

                var remainingScore = visit.ScoreBefore - visitScore;

                var isBust = _rules.IsBust(visit.ScoreBefore, visitScore, model.Segment, model.Multiplier, training.OutMode);

                if (isBust)
                {
                    visit.IsBust = true;
                    visit.ScoreAfter = visit.ScoreBefore;
                }
                else
                {
                    visit.ScoreAfter = remainingScore;
                }

                var isCheckout = visit.ScoreAfter == 0 && !visit.IsBust;
                var legsCompleted = training.Visits.Count(IsLegCheckout);
                var isSetFinished = isCheckout && legsCompleted >= training.NumberOfLegs;

                // Training endet erst mit dem letzten Leg des Satzes.
                if (isSetFinished)
                    training.TrainingSession.FinishedAt = DateTime.UtcNow;

                await UnitOfWork.SaveChangesAsync();

                var remainingDarts = 3 - model.ThrowNumber;

                return new X01TrainingThrowResult
                {
                    RemainingScore = visit.ScoreAfter,
                    RemainingDarts = remainingDarts,
                    IsBust = visit.IsBust,
                    IsCheckout = isCheckout,
                    LegNumber = visit.LegNumber,
                    LegsCompleted = legsCompleted,
                    NumberOfLegs = training.NumberOfLegs,
                    IsSetFinished = isSetFinished,
                    IsOpened = isOpened,
                    CheckoutSuggestions = isOpened && !isBust && !isCheckout && remainingDarts > 0
                        ? _rules.GetCheckoutSuggestions(visit.ScoreAfter, remainingDarts, training.OutMode)
                        : []
                };
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("", nameof(X01TrainingGame), exception);

                throw;
            }
        }

        public async Task FinishTrainingAsync(int trainingId)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var training = await LoadX01Training(trainingId, currentUser.UserId);

                if (training.TrainingSession.FinishedAt.HasValue)
                {
                    throw new Exception("Training is already finished.");
                }

                training.TrainingSession.FinishedAt = DateTime.UtcNow;

                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("", nameof(X01TrainingGame), exception);
                throw;
            }
        }

        public async Task<X01TrainingResult> GetResultAsync(int trainingId)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var training = await LoadX01Training(trainingId, currentUser.UserId);

                var visits = training.Visits
                    .OrderBy(v => v.VisitNumber)
                    .ToList();

                var throws = visits
                    .SelectMany(v => v.Throws)
                    .ToList();

                var totalThrows = throws.Count;

                // Gecheckte Legs: alle Aufnahmen eines Legs, das mit einem Checkout endet.
                var checkedOutLegs = visits
                    .GroupBy(v => v.LegNumber)
                    .Where(l => l.Any(IsLegCheckout))
                    .ToList();

                // Gewertete Punkte: Busts zählen 0, bei Double-In auch die Darts vor der Eröffnung.
                var totalScore = visits.Sum(v => v.ScoreBefore - v.ScoreAfter);
                var successfulVisits = visits.Count(v => !v.IsBust);
                var totalVisits = visits.Count;

                return new X01TrainingResult
                {
                    TrainingId = training.TrainingSessionId,
                    StartingScore = training.StartingScore,
                    InMode = training.InMode,
                    OutMode = training.OutMode,
                    RemainingScore = visits.LastOrDefault()?.ScoreAfter ?? training.StartingScore,
                    NumberOfLegs = training.NumberOfLegs,
                    LegsCompleted = checkedOutLegs.Count,
                    AverageDartsPerLeg = checkedOutLegs.Count == 0
                        ? null
                        : Math.Round((decimal)checkedOutLegs.Sum(l => l.Sum(v => v.Throws.Count)) / checkedOutLegs.Count, 2),
                    StartedAt = training.TrainingSession.StartedAt,
                    FinishedAt = training.TrainingSession.FinishedAt,
                    TotalVisits = totalVisits,
                    TotalThrows = totalThrows,
                    TotalScore = totalScore,
                    Busts = visits.Count(v => v.IsBust),
                    SuccessfulVisits = successfulVisits,
                    AverageScorePerThrow = totalThrows > 0
                        ? Math.Round((decimal)totalScore / totalThrows, 2)
                        : 0,
                    AverageScorePerVisit = totalVisits > 0
                        ? Math.Round((decimal)totalScore / totalVisits, 2)
                        : 0,
                    SingleHits = throws.Count(t => t.Multiplier == 1 && t.Segment != 0),
                    DoubleHits = throws.Count(t => t.Multiplier == 2),
                    TripleHits = throws.Count(t => t.Multiplier == 3),
                    Misses = throws.Count(t => t.Segment == 0)
                };
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("", nameof(X01TrainingGame), exception);

                throw;
            }
        }

        private static bool IsLegCheckout(TrainingGameVisitEntity visit) => visit.ScoreAfter == 0 && !visit.IsBust;
    }
}
