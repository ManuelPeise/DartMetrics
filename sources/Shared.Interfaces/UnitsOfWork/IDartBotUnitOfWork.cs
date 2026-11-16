using Data.DartContext.Entities.Bot;
using Shared.Interfaces.Repositories;

namespace Shared.Interfaces.UnitsOfWork
{
    public interface IDartBotUnitOfWork
    {
        IRepositoryBase<DartBotActiveModelEntity> DartBotActiveModelRepository { get; }
        IRepositoryBase<DartBotEntity> DartBotRepository { get; }
        IRepositoryBase<DartBotGameEntity> DartBotGameRepository { get; }
        IRepositoryBase<DartBotGameLegEntity> DartBotGameLegRepository { get; }
        IRepositoryBase<DartBotGamePlayerEntity> DartBotGamePlayerRepository { get; }
        IRepositoryBase<DartBotGameThrowEntity> DartBotGameThrowRepository { get; }
        IRepositoryBase<DartBotGameVisitEntity> DartBotGameVisitRepository { get; }
        IRepositoryBase<DartBotModelEntity> DartBotModelRepository { get; }
        IRepositoryBase<DartBotModelTrainingEntity> DartBotModelTrainingRepository { get; }
        IRepositoryBase<DartBotProfileVersionEntity> DartBotProfileVersionRepository { get; }
        IRepositoryBase<DartBotTrainingDatasetEntity> DartBotTrainingDatasetRepository { get; }
        IRepositoryBase<DartBotTrainingJobEntity> DartBotTrainingJobRepository { get; }

    }
}
