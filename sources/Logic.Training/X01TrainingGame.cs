using Data.DartContext.Entities.Training;
using Logic.Shared.Rules;
using Shared.Enums.Training;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.Training;
using Shared.Interfaces.UnitsOfWork;
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

        public async Task<int> StartTrainingAsync(StartX01TrainingModel model)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                if (model.StartingScore is not (ThreeZeroOneScore or FiveZeroOneScore))
                {
                    throw new Exception($"Starting score must be {ThreeZeroOneScore} or {FiveZeroOneScore}.");
                }

                var trainingSession = new TrainingSessionEntity
                {
                    TrainingType = TrainingTypeEnum.X01Training,
                    StartedAt = DateTime.UtcNow,
                    UserId = currentUser.UserId,
                    TrainingGame = new TrainingGameEntity
                    {
                        StartingScore = model.StartingScore
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

                    var scoreBefore = lastVisit?.ScoreAfter ?? training.StartingScore;

                    visit = new TrainingGameVisitEntity
                    {
                        VisitNumber = model.VisitNumber,
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

                var visitScore = visit.Throws.Sum(t => t.Score);
                var remainingScore = visit.ScoreBefore - visitScore;

                var isBust = remainingScore < 0 ||
                             remainingScore == 1 ||
                             (remainingScore == 0 &&
                              !_rules.IsDouble(model.Segment, model.Multiplier));

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

                if (isCheckout)
                    training.TrainingSession.FinishedAt = DateTime.UtcNow;

                await UnitOfWork.SaveChangesAsync();

                var remainingDarts = 3 - model.ThrowNumber;

                return new X01TrainingThrowResult
                {
                    RemainingScore = visit.ScoreAfter,
                    RemainingDarts = remainingDarts,
                    IsBust = visit.IsBust,
                    IsCheckout = isCheckout,
                    CheckoutSuggestions = !isBust && !isCheckout && remainingDarts > 0
                        ? _rules.GetCheckoutSuggestions(visit.ScoreAfter, remainingDarts)
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
                var totalScore = throws.Sum(t => t.Score);
                var successfulVisits = visits.Count(v => !v.IsBust);
                var totalVisits = visits.Count;

                return new X01TrainingResult
                {
                    TrainingId = training.Id,
                    StartingScore = training.StartingScore,
                    RemainingScore = visits.LastOrDefault()?.ScoreAfter ?? training.StartingScore,
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
    }
}
