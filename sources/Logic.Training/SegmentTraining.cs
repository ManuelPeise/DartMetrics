using Data.DartContext.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Training;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.Training;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Training;

namespace Logic.Training
{
    public class SegmentTraining : TrainingBase, ISegmentTraining
    {
        private const int MaxThrows = 3;

        public SegmentTraining(
            IUnitOfWork unitOfWork,
            IMessageLog messageLog,
            IClaimsHelper claimsHelper) : base(unitOfWork, messageLog, claimsHelper)
        {
        }

        public async Task<int> StartTrainingAsync(StartSegmentTrainingModel model)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                if (!IsValidSegment(model.TargetSegment) || !IsValidMultiplier(model.TargetSegment, model.TargetMultiplier) || model.NumberOfVisits <= 0)
                {
                    throw new ArgumentException("Invalid target segment, multiplier, or number of visits.");
                }

                var session = new TrainingSessionEntity
                {
                    UserId = currentUser.UserId,
                    StartedAt = DateTime.UtcNow,
                    TrainingType = TrainingTypeEnum.SegmentTraining,
                    SegmentTraining = new SegmentTrainingEntity
                    {
                        TargetSegment = model.TargetSegment,
                        TargetMultiplier = model.TargetMultiplier,
                        NumberOfVisits = model.NumberOfVisits
                    }
                };


                await UnitOfWork.DartTrainingUnitOfWork.TrainingSessionRepository.AddAsync(session);

                await UnitOfWork.SaveChangesAsync();

                return session.Id;
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error starting segment training.", nameof(SegmentTraining), exception);

                throw;
            }
        }

        public async Task<SegmentTrainingThrowResult> AddThrowAsync(SegmentTrainingThrow model)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var training = await LoadSegmentTraining(model.TrainingId, currentUser.UserId);

                if (training.TrainingSession.FinishedAt.HasValue)
                {
                    throw new ArgumentException("Training is already finished.");
                }

                if (model.VisitNumber < 1 || model.VisitNumber > training.NumberOfVisits)
                {
                    throw new ArgumentException($"Visit number must be between 1 and {training.NumberOfVisits}.");
                }

                if (!IsValidSegment(model.Segment) || !IsValidMultiplier(model.Segment, model.Multiplier))
                {
                    throw new ArgumentException("Invalid segment or multiplier.");
                }

                // Darts strikt der Reihe nach: Aufnahme 1 Dart 1–3, dann Aufnahme 2 Dart 1 …
                var (expectedVisit, expectedThrow) = NextThrowPosition(training);

                if (model.VisitNumber != expectedVisit || model.ThrowNumber != expectedThrow)
                {
                    throw new ArgumentException($"Expected visit {expectedVisit}, dart {expectedThrow}.");
                }

                var isHit = training.TargetSegment == model.Segment && training.TargetMultiplier == model.Multiplier;

                training.Throws.Add(new SegmentTrainingThrowEntity
                {
                    VisitNumber = model.VisitNumber,
                    ThrowNumber = model.ThrowNumber,
                    Segment = model.Segment,
                    Multiplier = model.Multiplier,
                    Score = model.Segment * model.Multiplier,
                    IsHit = isHit
                });

                var isVisitFinished = model.ThrowNumber == MaxThrows;
                var isFinished = isVisitFinished && model.VisitNumber == training.NumberOfVisits;

                if (isFinished)
                {
                    training.TrainingSession.FinishedAt = DateTime.UtcNow;
                }

                await UnitOfWork.SaveChangesAsync();

                return new SegmentTrainingThrowResult
                {
                    IsHit = isHit,
                    TotalHits = training.Throws.Count(t => t.IsHit),
                    TotalThrows = training.Throws.Count,
                    IsVisitFinished = isVisitFinished,
                    IsFinished = isFinished
                };
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error adding throw to segment training.", nameof(SegmentTraining), exception);

                throw;
            }
        }

        private static (int VisitNumber, int ThrowNumber) NextThrowPosition(SegmentTrainingEntity training)
        {
            var last = training.Throws
                .OrderBy(t => t.VisitNumber)
                .ThenBy(t => t.ThrowNumber)
                .LastOrDefault();

            if (last is null)
            {
                return (1, 1);
            }

            return last.ThrowNumber < MaxThrows
                ? (last.VisitNumber, last.ThrowNumber + 1)
                : (last.VisitNumber + 1, 1);
        }

        public async Task FinishTrainingAsync(int trainingId)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var trainingsEntity = await LoadSegmentTraining(trainingId, currentUser.UserId);

                if (trainingsEntity.TrainingSession.FinishedAt.HasValue)
                {
                    throw new InvalidOperationException($"Training with id: {trainingId} is already finished.");
                }

                trainingsEntity.TrainingSession.FinishedAt = DateTime.UtcNow;

                await UnitOfWork.SaveChangesAsync();

            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error finishing segment training.", nameof(SegmentTraining), exception);

                throw;
            }
        }

        public async Task<SegmentTrainingResult> GetResultAsync(int trainingId)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var trainingsEntity = await LoadSegmentTraining(trainingId, currentUser.UserId);

                return GetSegmentTrainingResult(trainingsEntity);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error getting segment training result.", nameof(SegmentTraining), exception);

                throw;
            }
        }




        private SegmentTrainingResult GetSegmentTrainingResult(SegmentTrainingEntity trainingEntity)
        {
            var throws = trainingEntity.Throws.ToList();

            var totalThrows = throws.Count;
            var totalHits = throws.Count(t => t.IsHit);
            var totalScore = throws.Sum(t => t.Score);

            var totalVisits = throws
                .Select(t => t.VisitNumber)
                .Distinct()
                .Count();

            return new SegmentTrainingResult
            {
                TrainingId = trainingEntity.TrainingSessionId,
                TargetSegment = trainingEntity.TargetSegment,
                TargetMultiplier = trainingEntity.TargetMultiplier,
                NumberOfVisits = trainingEntity.NumberOfVisits,
                StartedAt = trainingEntity.TrainingSession.StartedAt,
                FinishedAt = trainingEntity.TrainingSession.FinishedAt,
                TotalThrows = totalThrows,
                TotalHits = totalHits,
                TotalScore = totalScore,
                HitRate = totalThrows == 0 ? 0 :
                    Math.Round((decimal)totalHits / totalThrows * 100, 2),
                AverageScorePerThrow = totalThrows == 0 ? 0 :
                    Math.Round((decimal)totalScore / totalThrows, 2),
                AverageScorePerVisit = totalVisits == 0 ? 0 :
                    Math.Round((decimal)totalScore / totalVisits, 2),
                TotalVisits = totalVisits,
                SingleHits = throws.Count(t => t.IsHit && t.Multiplier == 1),
                DoubleHits = throws.Count(t => t.IsHit && t.Multiplier == 2),
                TripleHits = throws.Count(t => t.IsHit && t.Multiplier == 3),
                TargetHits = totalHits,
                Misses = throws.Count(t => t.Segment == 0),
                BestVisitScore = throws
                    .GroupBy(t => t.VisitNumber)
                    .Select(g => g.Sum(t => t.Score))
                    .DefaultIfEmpty(0)
                    .Max(),
                AverageHitsPerVisit = totalVisits == 0 ? 0 :
                    Math.Round((decimal)totalHits / totalVisits, 2)
            };
        }

    }
}
