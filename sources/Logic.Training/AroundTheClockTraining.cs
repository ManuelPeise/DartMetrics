using Data.DartContext.Entities.Training;
using Logic.Shared.Rules;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Training;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.Training;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Training;

namespace Logic.Training
{
    public class AroundTheClockTraining : TrainingBase, IAroundTheClockTraining
    {
        private readonly AroundTheClockRules _rules = new();

        public AroundTheClockTraining(
            IUnitOfWork unitOfWork,
            IMessageLog messageLog,
            IClaimsHelper claimsHelper) : base(unitOfWork, messageLog, claimsHelper) { }

        public async Task<int> StartTrainingAsync(StartAroundTheClockTrainingModel model)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var trainingSession = new TrainingSessionEntity
                {
                    TrainingType = TrainingTypeEnum.AroundTheClockTraining,
                    StartedAt = DateTime.UtcNow,
                    UserId = currentUser.UserId,
                    AroundTheClockTraining = new AroundTheClockTrainingEntity()
                };

                await UnitOfWork.DartTrainingUnitOfWork
                    .TrainingSessionRepository
                    .AddAsync(trainingSession);

                await UnitOfWork.SaveChangesAsync();

                return trainingSession.Id;
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error starting around the clock training.", nameof(AroundTheClockTraining), exception);

                throw;
            }
        }

        public async Task<AroundTheClockTrainingThrowResult> AddThrowAsync(AroundTheClockTrainingThrow model)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var training = await UnitOfWork.DartTrainingUnitOfWork
                    .AroundTheClockTrainingRepository
                    .GetFirstOrDefaultAsync(
                        t => t.Id == model.TrainingId,
                        include: t => t
                            .Include(t => t.TrainingSession)
                            .Include(t => t.Visits)
                            .ThenInclude(v => v.Throws));

                if (training == null || training.TrainingSession.UserId != currentUser.UserId)
                {
                    throw new Exception("Training not found.");
                }


                if (training.TrainingSession.FinishedAt != null)
                {
                    throw new Exception("Training is already finished.");
                }


                if (model.VisitNumber < 1)
                    throw new Exception("Invalid visit number.");

                if (model.ThrowNumber < 1 || model.ThrowNumber > 3)
                {
                    throw new Exception("Invalid throw number.");
                }


                if (!_rules.IsValidSegment(model.Segment) ||
                    !_rules.IsValidMultiplier(model.Segment, model.Multiplier))
                {
                    throw new Exception("Invalid dart throw.");
                }

                var orderedVisits = training.Visits
                    .OrderBy(v => v.VisitNumber)
                    .ToList();

                var lastVisit = orderedVisits.LastOrDefault();

                AroundTheClockTrainingVisitEntity visit;

                if (lastVisit == null)
                {
                    if (model.VisitNumber != 1 || model.ThrowNumber != 1)
                        throw new Exception("Invalid first throw.");

                    visit = new AroundTheClockTrainingVisitEntity
                    {
                        VisitNumber = 1
                    };

                    training.Visits.Add(visit);
                }
                else if (model.VisitNumber == lastVisit.VisitNumber)
                {
                    var lastThrow = lastVisit.Throws
                        .OrderBy(t => t.ThrowNumber)
                        .LastOrDefault();

                    if (lastThrow == null || model.ThrowNumber != lastThrow.ThrowNumber + 1)
                        throw new Exception("Invalid throw sequence.");

                    visit = lastVisit;
                }
                else if (model.VisitNumber == lastVisit.VisitNumber + 1)
                {
                    if (lastVisit.Throws.Count != 3 || model.ThrowNumber != 1)
                        throw new Exception("The previous visit is not completed.");

                    visit = new AroundTheClockTrainingVisitEntity
                    {
                        VisitNumber = model.VisitNumber
                    };

                    training.Visits.Add(visit);
                }
                else
                {
                    throw new Exception("Invalid visit sequence.");
                }

                var previousTarget = GetCurrentTarget(training);

                if (previousTarget == 23)
                    throw new Exception("Training is already completed.");

                var isHit = IsTargetHit(
                    previousTarget,
                    model.Segment,
                    model.Multiplier);

                var newTarget = isHit
                    ? GetNextTarget(previousTarget)
                    : previousTarget;

                var throwEntity = new AroundTheClockTrainingThrowEntity
                {
                    ThrowNumber = model.ThrowNumber,
                    Segment = model.Segment,
                    Multiplier = model.Multiplier,
                    Score = _rules.CalculateScore(model.Segment, model.Multiplier)
                };

                visit.Throws.Add(throwEntity);

                var isFinished = newTarget == 23;

                if (isFinished)
                    training.TrainingSession.FinishedAt = DateTime.UtcNow;

                await UnitOfWork.SaveChangesAsync();

                return new AroundTheClockTrainingThrowResult
                {
                    IsHit = isHit,
                    PreviousTarget = previousTarget,
                    CurrentTarget = newTarget,
                    IsFinished = isFinished
                };
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error processing around the clock training throw.", nameof(AroundTheClockTraining), exception);

                throw;
            }
        }

        public async Task FinishTrainingAsync(int trainingId)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var training = await UnitOfWork.DartTrainingUnitOfWork
                    .AroundTheClockTrainingRepository
                    .GetFirstOrDefaultAsync(
                        t => t.Id == trainingId,
                        include: t => t.Include(t => t.TrainingSession));

                if (training == null || training.TrainingSession.UserId != currentUser.UserId)
                {
                    throw new Exception("Training not found.");
                }

                if (training.TrainingSession.FinishedAt != null)
                {
                    throw new Exception("Training is already finished.");
                }

                training.TrainingSession.FinishedAt = DateTime.UtcNow;

                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error finishing around the clock training.", nameof(AroundTheClockTraining), exception);

                throw;
            }
        }

        public async Task<AroundTheClockTrainingResult> GetResultAsync(int trainingId)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var training = await UnitOfWork.DartTrainingUnitOfWork
                    .AroundTheClockTrainingRepository
                    .GetFirstOrDefaultAsync(
                        t => t.Id == trainingId,
                        include: t => t
                            .Include(t => t.TrainingSession)
                            .Include(t => t.Visits)
                            .ThenInclude(v => v.Throws));

                if (training == null || training.TrainingSession.UserId != currentUser.UserId)
                    throw new Exception("Training not found.");

                var throws = training.Visits
                    .OrderBy(v => v.VisitNumber)
                    .SelectMany(v => v.Throws.OrderBy(t => t.ThrowNumber))
                    .ToList();

                var totalThrows = throws.Count;

                var totalHits = 0;
                var singleHits = 0;
                var doubleHits = 0;
                var tripleHits = 0;
                var outerBullHits = 0;
                var innerBullHits = 0;

                var currentTarget = 1;

                foreach (var dart in throws)
                {
                    if (IsTargetHit(currentTarget, dart.Segment, dart.Multiplier))
                    {
                        totalHits++;

                        switch (dart.Multiplier)
                        {
                            case 1:
                                singleHits++;
                                break;
                            case 2:
                                doubleHits++;
                                break;
                            case 3:
                                tripleHits++;
                                break;
                        }

                        if (currentTarget == 21)
                            outerBullHits++;

                        if (currentTarget == 22)
                            innerBullHits++;

                        currentTarget = GetNextTarget(currentTarget);
                    }

                    if (currentTarget == 23)
                        break;
                }

                return new AroundTheClockTrainingResult
                {
                    TotalThrows = totalThrows,
                    TotalHits = totalHits,
                    TotalMisses = totalThrows - totalHits,

                    SingleHits = singleHits,
                    DoubleHits = doubleHits,
                    TripleHits = tripleHits,

                    OuterBullHits = outerBullHits,
                    InnerBullHits = innerBullHits,

                    TotalVisits = training.Visits.Count,

                    HitRate = totalThrows == 0
                        ? 0
                        : (double)totalHits / totalThrows * 100,

                    IsFinished = training.TrainingSession.FinishedAt.HasValue,
                    FinishedAt = training.TrainingSession.FinishedAt
                };

            }

            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error retrieving around the clock training result.", nameof(AroundTheClockTraining), exception);
                throw;
            }
        }

        private int GetCurrentTarget(AroundTheClockTrainingEntity training)
        {
            var currentTarget = 1;

            var throws = training.Visits
                .OrderBy(v => v.VisitNumber)
                .SelectMany(v => v.Throws.OrderBy(t => t.ThrowNumber));

            foreach (var dart in throws)
            {
                if (IsTargetHit(currentTarget, dart.Segment, dart.Multiplier))
                    currentTarget = GetNextTarget(currentTarget);

                if (currentTarget == 23)
                    break;
            }

            return currentTarget;
        }

        private bool IsTargetHit(int target, int segment, int multiplier)
        {
            if (target >= 1 && target <= 20)
                return segment == target;

            if (target == 21)
                return segment == 25 && multiplier == 1;

            if (target == 22)
                return segment == 25 && multiplier == 2;

            return false;
        }

        private int GetNextTarget(int currentTarget)
        {
            if (currentTarget >= 1 && currentTarget < 22)
                return currentTarget + 1;

            if (currentTarget == 22)
                return 23;

            return 23;
        }


    }
}
