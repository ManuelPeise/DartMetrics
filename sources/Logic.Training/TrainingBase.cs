using Data.DartContext.Entities.Training;
using Logic.Shared;
using Microsoft.EntityFrameworkCore;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Authentication;

namespace Logic.Training
{
    public abstract class TrainingBase: ALogicBase
    {
        private readonly IClaimsHelper _claimsHelper;

        
        protected TrainingBase(IUnitOfWork unitOfWork, IMessageLog messageLog, IClaimsHelper claimsHelper): base(unitOfWork, messageLog)
        {
            _claimsHelper = claimsHelper;
        }

        protected void EnsureCurrentUserIsValid(out CurrentUser currentUser)
        {
            currentUser = _claimsHelper.GetCurrentUserFromClaims();

            if (currentUser == null || currentUser.UserId == 0)
            {
                throw new Exception("Current user not found.");
            }
        }

        // Öffentliche TrainingId ist immer die Id der TrainingSession (Rückgabe von Start…).
        // Geladen wird daher über TrainingSessionId; der Zugriff wird über den Benutzer der Session geprüft.

        protected async Task<SegmentTrainingEntity> LoadSegmentTraining(int trainingId, int userId)
        {
            var trainingEntity = await UnitOfWork.DartTrainingUnitOfWork.SegmentTrainingRepository.GetFirstOrDefaultAsync(
                t => t.TrainingSessionId == trainingId && t.TrainingSession.UserId == userId,
                include: q => q.Include(t => t.TrainingSession).Include(t => t.Throws));

            return trainingEntity ?? throw NotFound(trainingId);
        }

        protected async Task<CheckoutTrainingEntity> LoadCheckoutTraining(int trainingId, int userId)
        {
            var trainingEntity = await UnitOfWork.DartTrainingUnitOfWork.CheckoutTrainingRepository.GetFirstOrDefaultAsync(
                t => t.TrainingSessionId == trainingId && t.TrainingSession.UserId == userId,
                include: q => q
                    .Include(t => t.TrainingSession)
                    .Include(t => t.Attempts)
                    .ThenInclude(a => a.Throws));

            return trainingEntity ?? throw NotFound(trainingId);
        }

        protected async Task<TrainingGameEntity> LoadX01Training(int trainingId, int userId)
        {
            var trainingEntity = await UnitOfWork.DartTrainingUnitOfWork.TrainingGameRepository.GetFirstOrDefaultAsync(
                t => t.TrainingSessionId == trainingId && t.TrainingSession.UserId == userId,
                include: q => q
                    .Include(t => t.TrainingSession)
                    .Include(t => t.Visits)
                    .ThenInclude(v => v.Throws));

            return trainingEntity ?? throw NotFound(trainingId);
        }

        protected async Task<AroundTheClockTrainingEntity> LoadAroundTheClockTraining(int trainingId, int userId)
        {
            var trainingEntity = await UnitOfWork.DartTrainingUnitOfWork.AroundTheClockTrainingRepository.GetFirstOrDefaultAsync(
                t => t.TrainingSessionId == trainingId && t.TrainingSession.UserId == userId,
                include: q => q
                    .Include(t => t.TrainingSession)
                    .Include(t => t.Visits)
                    .ThenInclude(v => v.Throws));

            return trainingEntity ?? throw NotFound(trainingId);
        }

        private static KeyNotFoundException NotFound(int trainingId) =>
            new($"Training with id {trainingId} was not found.");

        protected static bool IsValidSegment(int segment)
        {
            return segment == 0
                || (segment >= 1 && segment <= 20)
                || segment == 25;
        }

        protected bool IsValidMultiplier(int segment, int multiplier)
        {
            switch (segment)
            {
                case 0:
                    return multiplier == 0;
                case 25:
                    return multiplier == 1 || multiplier == 2;
                default:
                    return multiplier >= 1 && multiplier <= 3;
            }
        }
    }
}
