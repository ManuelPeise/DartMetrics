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

        protected async Task<SegmentTrainingEntity> LoadTraining(int trainingId, int userId)
        {
            var trainingEntity = await UnitOfWork.DartTrainingUnitOfWork.SegmentTrainingRepository.GetFirstOrDefaultAsync(t => t.Id == trainingId, include: t => t.Include(t => t.TrainingSession).Include(t => t.Throws));

            if (trainingEntity == null || trainingEntity.TrainingSession.UserId != userId)
            {
                throw new Exception($"Training with id: {trainingId} for user id: {userId} not found.");
            }

            return trainingEntity;
        }

        protected async Task<TrainingGameEntity> LoadX01Training(int trainingId, int userId)
        {
            var trainingEntity = await UnitOfWork.DartTrainingUnitOfWork.TrainingGameRepository
                .GetFirstOrDefaultAsync(
                    t => t.Id == trainingId,
                    include: t => t
                        .Include(t => t.TrainingSession)
                        .Include(t => t.Visits)
                        .ThenInclude(v => v.Throws));

            if (trainingEntity == null || trainingEntity.TrainingSession.UserId != userId)
            {
                throw new Exception($"Training with id: {trainingId} for user id: {userId} not found.");
            }

            return trainingEntity;
        }

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
