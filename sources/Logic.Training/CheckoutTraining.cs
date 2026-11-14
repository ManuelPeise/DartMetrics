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
    public class CheckoutTraining : TrainingBase, ICheckoutTraining
    {
        private const int MinTargetScore = 2;
        private const int MaxTargetScore = 170;
        private const int DartsPerAttempt = 3;

        // Checkout-Training bleibt bewusst Double-Out.
        private readonly X01Rules _rules = new();

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

        /// <summary>
        /// Ein Versuch = bis zu drei Darts auf den Zielwert (Double-Out). Er endet mit Checkout, Bust oder dem dritten Dart;
        /// danach beginnt der nächste Dart einen neuen Versuch (AttemptNumber + 1).
        /// </summary>
        public async Task<CheckoutTrainingThrowResult> AddThrowAsync(CheckoutTrainingThrow model)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var training = await LoadCheckoutTraining(model.TrainingId, currentUser.UserId);

                if (training.TrainingSession.FinishedAt.HasValue)
                {
                    throw new ArgumentException("Training is already finished.");
                }

                if (!IsValidSegment(model.Segment) || !IsValidMultiplier(model.Segment, model.Multiplier))
                {
                    throw new ArgumentException("Invalid segment or multiplier.");
                }

                var lastAttempt = training.Attempts.OrderBy(a => a.AttemptNumber).LastOrDefault();
                var lastAttemptOpen = lastAttempt is not null && !IsAttemptFinished(lastAttempt, training.TargetScore);

                // Erwartete Position: laufender Versuch fortsetzen oder neuen Versuch beginnen.
                var expectedAttempt = lastAttempt is null ? 1 : lastAttemptOpen ? lastAttempt.AttemptNumber : lastAttempt.AttemptNumber + 1;
                var expectedThrow = lastAttemptOpen ? lastAttempt!.Throws.Count + 1 : 1;

                if (model.AttemptNumber != expectedAttempt || model.ThrowNumber != expectedThrow)
                {
                    throw new ArgumentException($"Expected attempt {expectedAttempt}, dart {expectedThrow}.");
                }

                var attempt = lastAttemptOpen ? lastAttempt! : new CheckoutTrainingAttemptEntity { AttemptNumber = expectedAttempt };

                if (!lastAttemptOpen)
                {
                    training.Attempts.Add(attempt);
                }

                attempt.Throws.Add(new CheckoutTrainingThrowEntity
                {
                    ThrowNumber = model.ThrowNumber,
                    Segment = model.Segment,
                    Multiplier = model.Multiplier,
                    Score = _rules.CalculateScore(model.Segment, model.Multiplier)
                });

                var visitScore = attempt.Throws.Sum(t => t.Score);
                var isBust = _rules.IsBust(training.TargetScore, visitScore, model.Segment, model.Multiplier);
                var isCheckout = _rules.IsCheckout(training.TargetScore, visitScore, model.Segment, model.Multiplier);
                var remainingDarts = DartsPerAttempt - attempt.Throws.Count;

                attempt.IsCheckout = isCheckout;

                await UnitOfWork.SaveChangesAsync();

                var remaining = isBust ? training.TargetScore : training.TargetScore - visitScore;
                var isAttemptFinished = isBust || isCheckout || remainingDarts == 0;

                return new CheckoutTrainingThrowResult
                {
                    RemainingScore = remaining,
                    RemainingDarts = isAttemptFinished ? 0 : remainingDarts,
                    IsBust = isBust,
                    IsCheckout = isCheckout,
                    IsAttemptFinished = isAttemptFinished,
                    TotalAttempts = training.Attempts.Count,
                    SuccessfulAttempts = training.Attempts.Count(a => IsSuccessful(a, training.TargetScore)),
                    CheckoutSuggestions = isAttemptFinished
                        ? []
                        : _rules.GetCheckoutSuggestions(remaining, remainingDarts)
                };
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

                var training = await LoadCheckoutTraining(trainingId, currentUser.UserId);

                if (training.TrainingSession.FinishedAt.HasValue)
                {
                    throw new ArgumentException($"Training with id {trainingId} is already finished.");
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

                var training = await LoadCheckoutTraining(trainingId, currentUser.UserId);

                var attempts = training.Attempts.ToList();
                var successfulAttempts = attempts.Where(a => IsSuccessful(a, training.TargetScore)).ToList();
                var allThrows = attempts.SelectMany(a => a.Throws).ToList();

                var totalAttempts = attempts.Count;
                var totalThrows = allThrows.Count;
                var totalScore = allThrows.Sum(t => t.Score);
                var successfulCount = successfulAttempts.Count;

                return new CheckoutTrainingResult
                {
                    TrainingId = training.TrainingSessionId,
                    TargetScore = training.TargetScore,
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
                        : Math.Round((decimal)successfulAttempts.Sum(a => a.Throws.Count) / successfulCount, 2)
                };
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error getting checkout training result.", nameof(CheckoutTraining), exception);

                throw;
            }
        }

        /// <summary>Checkout: Summe trifft genau den Zielwert, letzter Dart ein Double (Bull zählt als Double).</summary>
        private bool IsSuccessful(CheckoutTrainingAttemptEntity attempt, int targetScore)
        {
            var throws = attempt.Throws.OrderBy(t => t.ThrowNumber).ToList();

            return throws.Count > 0 &&
                   _rules.IsCheckout(targetScore, throws.Sum(t => t.Score), throws[^1].Segment, throws[^1].Multiplier);
        }

        private bool IsAttemptFinished(CheckoutTrainingAttemptEntity attempt, int targetScore)
        {
            var throws = attempt.Throws.OrderBy(t => t.ThrowNumber).ToList();

            if (throws.Count >= DartsPerAttempt)
            {
                return true;
            }

            if (throws.Count == 0)
            {
                return false;
            }

            var score = throws.Sum(t => t.Score);
            var last = throws[^1];

            return _rules.IsBust(targetScore, score, last.Segment, last.Multiplier) ||
                   _rules.IsCheckout(targetScore, score, last.Segment, last.Multiplier);
        }
    }
}
