using Data.DartContext;
using Data.DartContext.Entities.Training;
using Logic.Shared.Repositories;
using Shared.Interfaces.Repositories;
using Shared.Interfaces.UnitsOfWork;

namespace Logic.Shared.UnitsOfWork
{
    public class DartTrainingUnitOfWork : IDartTrainingUnitOfWork
    {
        private readonly DartDatabaseContext _dartDatabaseContext;

        // Training session
        public IRepositoryBase<TrainingSessionEntity> TrainingSessionRepository { get; }

        // Checkout training
        public IRepositoryBase<CheckoutTrainingEntity> CheckoutTrainingRepository { get; }

        public IRepositoryBase<CheckoutTrainingAttemptEntity> CheckoutTrainingAttemptRepository { get; }

        public IRepositoryBase<CheckoutTrainingThrowEntity> CheckoutTrainingThrowRepository { get; }

        // Segment training
        public IRepositoryBase<SegmentTrainingEntity> SegmentTrainingRepository { get; }

        public IRepositoryBase<SegmentTrainingThrowEntity> SegmentTrainingThrowRepository { get; }

        // Training games
        public IRepositoryBase<TrainingGameEntity> TrainingGameRepository { get; }

        public IRepositoryBase<TrainingGameThrowEntity> TrainingGameThrowRepository { get; }

        public IRepositoryBase<TrainingGameVisitEntity> TrainingGameVisitRepository { get; }

        // Around the clock training
        public IRepositoryBase<AroundTheClockTrainingEntity> AroundTheClockTrainingRepository { get; }
        public IRepositoryBase<AroundTheClockTrainingVisitEntity> AroundTheClockTrainingVisitRepository { get; }
        public IRepositoryBase<AroundTheClockTrainingThrowEntity> AroundTheClockTrainingThrowRepository { get; }


        public DartTrainingUnitOfWork(DartDatabaseContext dartDatabaseContext)
        {
            _dartDatabaseContext = dartDatabaseContext;

            TrainingSessionRepository = new RepositoryBase<TrainingSessionEntity>(_dartDatabaseContext);
            CheckoutTrainingRepository = new RepositoryBase<CheckoutTrainingEntity>(_dartDatabaseContext);
            CheckoutTrainingAttemptRepository = new RepositoryBase<CheckoutTrainingAttemptEntity>(_dartDatabaseContext);
            CheckoutTrainingThrowRepository = new RepositoryBase<CheckoutTrainingThrowEntity>(_dartDatabaseContext);
            SegmentTrainingRepository = new RepositoryBase<SegmentTrainingEntity>(_dartDatabaseContext);
            SegmentTrainingThrowRepository = new RepositoryBase<SegmentTrainingThrowEntity>(_dartDatabaseContext);
            TrainingGameRepository = new RepositoryBase<TrainingGameEntity>(_dartDatabaseContext);
            TrainingGameThrowRepository = new RepositoryBase<TrainingGameThrowEntity>(_dartDatabaseContext);
            TrainingGameVisitRepository = new RepositoryBase<TrainingGameVisitEntity>(_dartDatabaseContext);
            AroundTheClockTrainingRepository = new RepositoryBase<AroundTheClockTrainingEntity>(_dartDatabaseContext);
            AroundTheClockTrainingVisitRepository = new RepositoryBase<AroundTheClockTrainingVisitEntity>(_dartDatabaseContext);
            AroundTheClockTrainingThrowRepository = new RepositoryBase<AroundTheClockTrainingThrowEntity>(_dartDatabaseContext);
        }
    }
}
