using Data.DartContext.Entities.Bot;
using Shared.Interfaces.Repositories;

namespace Shared.Interfaces.UnitsOfWork
{
    public interface IDartBotUnitOfWork
    {
        IRepositoryBase<DartBotEntity> DartBotRepository { get; }
        IRepositoryBase<DartBotGameEntity> DartBotGameRepository { get; }
        IRepositoryBase<DartBotGameLegEntity> DartBotGameLegRepository { get; }
        IRepositoryBase<DartBotGamePlayerEntity> DartBotGamePlayerRepository { get; }
        IRepositoryBase<DartBotGameThrowEntity> DartBotGameThrowRepository { get; }
        IRepositoryBase<DartBotGameVisitEntity> DartBotGameVisitRepository { get; }
        IRepositoryBase<DartBotProfileVersionEntity> DartBotProfileVersionRepository { get; }

    }
}
