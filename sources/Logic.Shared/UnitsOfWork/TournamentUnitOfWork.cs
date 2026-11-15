using Data.DartContext;
using Data.DartContext.Entities.Tournament;
using Logic.Shared.Repositories;
using Shared.Interfaces.Repositories;
using Shared.Interfaces.UnitsOfWork;

namespace Logic.Shared.UnitsOfWork
{
    public class TournamentUnitOfWork : ITournamentUnitOfWork
    {
        public IRepositoryBase<TournamentEntity> TournamentRepository { get; }

        public IRepositoryBase<TournamentParticipantEntity> TournamentParticipantRepository { get; }

        public IRepositoryBase<TournamentRoundEntity> TournamentRoundRepository { get; }

        public IRepositoryBase<TournamentMatchEntity> TournamentMatchRepository { get; }

        public IRepositoryBase<TournamentMatchLegEntity> TournamentMatchLegRepository { get; }

        public IRepositoryBase<TournamentMatchVisitEntity> TournamentMatchVisitRepository { get; }

        public IRepositoryBase<TournamentMatchThrowEntity> TournamentMatchThrowRepository { get; }

        public TournamentUnitOfWork(DartDatabaseContext context)
        {
            TournamentRepository = new RepositoryBase<TournamentEntity>(context);
            TournamentParticipantRepository = new RepositoryBase<TournamentParticipantEntity>(context);
            TournamentRoundRepository = new RepositoryBase<TournamentRoundEntity>(context);
            TournamentMatchRepository = new RepositoryBase<TournamentMatchEntity>(context);
            TournamentMatchLegRepository = new RepositoryBase<TournamentMatchLegEntity>(context);
            TournamentMatchVisitRepository = new RepositoryBase<TournamentMatchVisitEntity>(context);
            TournamentMatchThrowRepository = new RepositoryBase<TournamentMatchThrowEntity>(context);
        }
    }
}
