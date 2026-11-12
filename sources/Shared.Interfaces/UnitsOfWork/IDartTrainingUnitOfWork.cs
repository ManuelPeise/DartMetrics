using Data.DartContext.Configurations.Training;
using Data.DartContext.Entities.Training;
using Shared.Interfaces.Repositories;

namespace Shared.Interfaces.UnitsOfWork
{
    public interface IDartTrainingUnitOfWork
    {
        IRepositoryBase<TrainingSessionEntity> TrainingSessionRepository { get; }

        IRepositoryBase<CheckoutTrainingEntity> CheckoutTrainingRepository { get; }
        IRepositoryBase<CheckoutTrainingAttemptEntity> CheckoutTrainingAttemptRepository { get; }
        IRepositoryBase<CheckoutTrainingThrowEntity> CheckoutTrainingThrowRepository { get; }

        IRepositoryBase<SegmentTrainingEntity> SegmentTrainingRepository { get; }
        IRepositoryBase<SegmentTrainingThrowEntity> SegmentTrainingThrowRepository { get; }

        IRepositoryBase<TrainingGameEntity> TrainingGameRepository { get; }
        IRepositoryBase<TrainingGameThrowEntity> TrainingGameThrowRepository { get; }
        IRepositoryBase<TrainingGameVisitEntity> TrainingGameVisitRepository { get; }
    }
}
