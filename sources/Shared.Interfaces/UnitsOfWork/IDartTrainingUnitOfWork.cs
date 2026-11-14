using Data.DartContext.Configurations.Training;
using Data.DartContext.Entities.Training;
using Shared.Interfaces.Repositories;

namespace Shared.Interfaces.UnitsOfWork
{
    public interface IDartTrainingUnitOfWork
    {
        // Training session
        IRepositoryBase<TrainingSessionEntity> TrainingSessionRepository { get; }

        // Checkout training
        IRepositoryBase<CheckoutTrainingEntity> CheckoutTrainingRepository { get; }
        IRepositoryBase<CheckoutTrainingAttemptEntity> CheckoutTrainingAttemptRepository { get; }
        IRepositoryBase<CheckoutTrainingThrowEntity> CheckoutTrainingThrowRepository { get; }

        // Segment training
        IRepositoryBase<SegmentTrainingEntity> SegmentTrainingRepository { get; }
        IRepositoryBase<SegmentTrainingThrowEntity> SegmentTrainingThrowRepository { get; }

        // Around the clock training
        IRepositoryBase<AroundTheClockTrainingEntity> AroundTheClockTrainingRepository { get; }
        IRepositoryBase<AroundTheClockTrainingVisitEntity> AroundTheClockTrainingVisitRepository { get; }
        IRepositoryBase<AroundTheClockTrainingThrowEntity> AroundTheClockTrainingThrowRepository { get; }

        // Training games
        IRepositoryBase<TrainingGameEntity> TrainingGameRepository { get; }
        IRepositoryBase<TrainingGameThrowEntity> TrainingGameThrowRepository { get; }
        IRepositoryBase<TrainingGameVisitEntity> TrainingGameVisitRepository { get; }
    }
}
