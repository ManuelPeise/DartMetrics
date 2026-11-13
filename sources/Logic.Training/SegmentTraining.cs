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

        public async Task AddThrowAsync(SegmentTrainingThrow model)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var training = await LoadTraining(model.TrainingId, currentUser.UserId);

                if (IsTrainingFinishedOrMaxVisited(training, model.VisitNumber) ||
                    !IsValidThrow(training, model.ThrowNumber, model.VisitNumber) ||
                    !IsValidSegment(model.Segment) ||
                    !IsValidMultiplier(model.Segment, model.Multiplier))
                {
                    throw new Exception("Invalid throw, segment, or multiplier.");
                }

                var score = model.Segment * model.Multiplier;

                var isHit =
                    training.TargetSegment == model.Segment &&
                    training.TargetMultiplier == model.Multiplier;

                training.Throws.Add(new SegmentTrainingThrowEntity
                {
                    VisitNumber = model.VisitNumber,
                    ThrowNumber = model.ThrowNumber,
                    Segment = model.Segment,
                    Multiplier = model.Multiplier,
                    Score = score,
                    IsHit = isHit
                });

                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error adding throw to segment training.", nameof(SegmentTraining), exception);
            }
        }

        public async Task FinishTrainingAsync(int trainingId)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var trainingsEntity = await LoadTraining(trainingId, currentUser.UserId);

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

                var trainingsEntity = await LoadTraining(trainingId, currentUser.UserId);

                return GetSegmentTrainingResult(trainingsEntity);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error getting segment training result.", nameof(SegmentTraining), exception);

                throw;
            }
        }

        private bool IsTrainingFinishedOrMaxVisited(SegmentTrainingEntity trainingEntity, int visited)
        {
            return trainingEntity.TrainingSession.FinishedAt.HasValue || visited >= trainingEntity.NumberOfVisits;
        }

        private bool IsExistingThrow(SegmentTrainingEntity entity, int throwNumber, int numberOfVisits)
        {
            return entity.Throws.Any(t => t.ThrowNumber == throwNumber || t.ThrowNumber >= numberOfVisits);
        }

        private bool IsValidThrow(SegmentTrainingEntity entity, int throwNumber, int numberOfVisits)
        {
            return entity.Throws.Count < MaxThrows || !IsExistingThrow(entity, throwNumber, numberOfVisits);
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
                TrainingId = trainingEntity.Id,
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
