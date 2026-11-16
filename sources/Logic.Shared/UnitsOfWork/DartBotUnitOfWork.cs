using Data.DartContext;
using Data.DartContext.Entities.Bot;
using Logic.Shared.Repositories;
using Shared.Interfaces.Repositories;
using Shared.Interfaces.UnitsOfWork;

namespace Logic.Shared.UnitsOfWork
{
    public class DartBotUnitOfWork: IDartBotUnitOfWork
    {
        private readonly DartDatabaseContext _context;


        public IRepositoryBase<DartBotEntity> DartBotRepository { get; }

        public IRepositoryBase<DartBotGameEntity> DartBotGameRepository { get; }

        public IRepositoryBase<DartBotGameLegEntity> DartBotGameLegRepository { get; }

        public IRepositoryBase<DartBotGamePlayerEntity> DartBotGamePlayerRepository { get; }

        public IRepositoryBase<DartBotGameThrowEntity> DartBotGameThrowRepository { get; }

        public IRepositoryBase<DartBotGameVisitEntity> DartBotGameVisitRepository { get; }


        public IRepositoryBase<DartBotProfileVersionEntity> DartBotProfileVersionRepository { get; }



        public DartBotUnitOfWork(DartDatabaseContext context)
        {
            _context = context;
            
            DartBotRepository = new RepositoryBase<DartBotEntity>(_context);
            DartBotGameRepository = new RepositoryBase<DartBotGameEntity>(_context);
            DartBotGameLegRepository = new RepositoryBase<DartBotGameLegEntity>(_context);
            DartBotGamePlayerRepository = new RepositoryBase<DartBotGamePlayerEntity>(_context);
            DartBotGameThrowRepository = new RepositoryBase<DartBotGameThrowEntity>(_context);
            DartBotGameVisitRepository = new RepositoryBase<DartBotGameVisitEntity>(_context);
            DartBotProfileVersionRepository = new RepositoryBase<DartBotProfileVersionEntity>(_context);
        }
    }
}
