using Data.DartContext.Entities.Training;
using Logic.Shared;
using Microsoft.EntityFrameworkCore;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.UnitsOfWork;
using Shared.Enums.Training;
using Shared.Models.Authentication;
using Shared.Models.Training;

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
                throw new UnauthorizedAccessException("Current user not found.");
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

        protected async Task<Bobs27TrainingEntity> LoadBobs27Training(int trainingId, int userId)
        {
            var trainingEntity = await UnitOfWork.DartTrainingUnitOfWork.Bobs27TrainingRepository.GetFirstOrDefaultAsync(
                t => t.TrainingSessionId == trainingId && t.TrainingSession.UserId == userId,
                include: q => q
                    .Include(t => t.TrainingSession)
                    .Include(t => t.Players)
                    .Include(t => t.Turns));

            return trainingEntity ?? throw NotFound(trainingId);
        }

        /// <summary>Id der neuesten offenen Session dieser Art, sonst null.</summary>
        protected async Task<int?> FindRunningSessionIdAsync(int userId, TrainingTypeEnum trainingType)
        {
            var sessions = await UnitOfWork.DartTrainingUnitOfWork.TrainingSessionRepository.GetWhereAsync(
                s => s.UserId == userId && s.TrainingType == trainingType && s.FinishedAt == null,
                asNoTracking: true);

            return sessions.OrderByDescending(s => s.StartedAt).ThenByDescending(s => s.Id).FirstOrDefault()?.Id;
        }

        /// <summary>
        /// Vor dem Start eines neuen Trainings: noch offene Trainings derselben Art beenden (z. B. aus einem zweiten
        /// Tab oder Gerät), damit keine Sessions für immer offen bleiben. Gespeichert wird mit dem neuen Training.
        /// </summary>
        protected async Task FinishOpenSessionsAsync(int userId, TrainingTypeEnum trainingType)
        {
            var open = await UnitOfWork.DartTrainingUnitOfWork.TrainingSessionRepository.GetWhereAsync(
                s => s.UserId == userId && s.TrainingType == trainingType && s.FinishedAt == null);

            var now = DateTime.UtcNow;
            foreach (var session in open)
            {
                session.FinishedAt = now;
            }
        }

        protected static List<TrainingDart> ToDarts(IEnumerable<(int Segment, int Multiplier)> darts) =>
            darts.Select(d => new TrainingDart { Segment = d.Segment, Multiplier = d.Multiplier }).ToList();

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
