using Data.DartContext.Entities.Training;
using Shared.Enums.Training;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.Training;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Training;

namespace Logic.Training
{
    public class CheckoutTraining : TrainingBase, ICheckoutTraining
    {
        private const int MinTargetScore = 2;
        private const int MaxTargetScore = 170;

        public CheckoutTraining(
            IUnitOfWork unitOfWork,
            IMessageLog messageLog,
            IClaimsHelper claimsHelper) : base(unitOfWork, messageLog, claimsHelper)
        {
        }

        public async Task<int> StartTrainingAsync(StartCheckoutTrainingModel model)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                if (model.TargetScore < MinTargetScore || model.TargetScore > MaxTargetScore)
                {
                    throw new ArgumentException("Invalid checkout target score.");
                }

                var session = new TrainingSessionEntity
                {
                    UserId = currentUser.UserId,
                    StartedAt = DateTime.UtcNow,
                    TrainingType = TrainingTypeEnum.CheckoutTraining,
                    CheckoutTraining = new CheckoutTrainingEntity
                    {
                        TargetScore = model.TargetScore
                    }
                };

                await UnitOfWork.DartTrainingUnitOfWork.TrainingSessionRepository.AddAsync(session);
                await UnitOfWork.SaveChangesAsync();

                return session.Id;

            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error starting checkout training.", nameof(CheckoutTraining), exception);

                throw;
            }

        }

        public async Task AddThrowAsync(CheckoutTrainingThrow model)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var training = await LoadTraining(model.TrainingId, currentUser.UserId);

                if (training.TrainingSession.FinishedAt.HasValue || training.TrainingSession.CheckoutTraining == null)
                {
                    throw new InvalidOperationException("Training is already finished or checkout training is not available.");
                }

                if (model.AttemptNumber <= 0 ||
                    model.ThrowNumber < 1 ||
                    model.ThrowNumber > 3 ||
                    !IsValidSegment(model.Segment) ||
                    !IsValidMultiplier(model.Segment, model.Multiplier))
                {
                    throw new ArgumentException("Invalid checkout throw.");
                }

                var attempt = training.TrainingSession.CheckoutTraining.Attempts
                    .FirstOrDefault(a => a.AttemptNumber == model.AttemptNumber);

                if (attempt == null)
                {
                    attempt = new CheckoutTrainingAttemptEntity
                    {
                        AttemptNumber = model.AttemptNumber,
                        Throws = []
                    };

                    training.TrainingSession.CheckoutTraining.Attempts.Add(attempt);
                }

                if (attempt.Throws.Any(t => t.ThrowNumber == model.ThrowNumber))
                {
                    throw new InvalidOperationException("Throw already exists.");
                }

                if (attempt.Throws.Count >= 3)
                {
                    throw new InvalidOperationException("Attempt already contains three throws.");
                }

                attempt.Throws.Add(new CheckoutTrainingThrowEntity
                {
                    ThrowNumber = model.ThrowNumber,
                    Segment = model.Segment,
                    Multiplier = model.Multiplier,
                    Score = model.Segment * model.Multiplier
                });

                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error adding throw to checkout training.", nameof(CheckoutTraining), exception);

                throw;
            }
        }

        public async Task FinishTrainingAsync(int trainingId)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var training = await LoadTraining(trainingId, currentUser.UserId);

                if (training.TrainingSession.FinishedAt.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Training with id {trainingId} is already finished.");
                }

                training.TrainingSession.FinishedAt = DateTime.UtcNow;

                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error finishing checkout training.", nameof(CheckoutTraining), exception);

                throw;
            }
        }

        public async Task<CheckoutTrainingResult> GetResultAsync(int trainingId)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var training = await LoadTraining(trainingId, currentUser.UserId);

                if(training.TrainingSession.CheckoutTraining == null)
                {
                    throw new InvalidOperationException("Checkout training data is not available.");
                }

                var attempts = training.TrainingSession.CheckoutTraining.Attempts.ToList();

                var successfulAttempts = attempts
                    .Where(a =>
                    {
                        var throws = a.Throws.OrderBy(t => t.ThrowNumber).ToList();

                        return throws.Count > 0 &&
                               throws.Count <= 3 &&
                               throws.Sum(t => t.Score) == training.TrainingSession.CheckoutTraining.TargetScore &&
                               throws.Last().Multiplier == 2;
                    })
                    .ToList();

                var allThrows = attempts
                    .SelectMany(a => a.Throws)
                    .ToList();

                var totalAttempts = attempts.Count;
                var totalThrows = allThrows.Count;
                var totalScore = allThrows.Sum(t => t.Score);
                var successfulCount = successfulAttempts.Count;

                return GetCheckoutTrainingResult(training, successfulAttempts, totalAttempts, successfulCount, totalThrows, totalScore);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage(
                    "Error getting checkout training result.",
                    nameof(CheckoutTraining),
                    exception);

                throw;
            }
        }

        private CheckoutTrainingResult GetCheckoutTrainingResult(SegmentTrainingEntity training, List<CheckoutTrainingAttemptEntity> successfulAttempts, int totalAttempts, int successfulCount, int totalThrows, int totalScore)
        {
            if(training.TrainingSession.CheckoutTraining == null)
            {
                throw new InvalidOperationException("Checkout training data is not available.");
            }

            return new CheckoutTrainingResult
            {
                TrainingId = training.Id,
                TargetScore = training.TrainingSession.CheckoutTraining.TargetScore,
                StartedAt = training.TrainingSession.StartedAt,
                FinishedAt = training.TrainingSession.FinishedAt,
                TotalAttempts = totalAttempts,
                SuccessfulAttempts = successfulCount,
                FailedAttempts = totalAttempts - successfulCount,
                CheckoutRate = totalAttempts == 0
                        ? 0
                        : Math.Round((decimal)successfulCount / totalAttempts * 100, 2),
                TotalThrows = totalThrows,
                TotalScore = totalScore,
                AverageScorePerThrow = totalThrows == 0
                        ? 0
                        : Math.Round((decimal)totalScore / totalThrows, 2),
                AverageThrowsPerAttempt = totalAttempts == 0
                        ? 0
                        : Math.Round((decimal)totalThrows / totalAttempts, 2),
                BestCheckout = successfulAttempts.Count == 0
                        ? 0
                        : successfulAttempts.Max(a => a.Throws.Sum(t => t.Score)),
                FewestDartsForCheckout = successfulAttempts.Count == 0
                        ? 0
                        : successfulAttempts.Min(a => a.Throws.Count),
                AverageDartsPerCheckout = successfulCount == 0
                        ? 0
                        : Math.Round(
                            (decimal)successfulAttempts.Sum(a => a.Throws.Count) / successfulCount, 2)
            };
        }
    }
}
