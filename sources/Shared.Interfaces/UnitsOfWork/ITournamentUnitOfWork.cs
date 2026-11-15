using Data.DartContext.Entities.Tournament;
using Shared.Interfaces.Repositories;

namespace Shared.Interfaces.UnitsOfWork
{
    public interface ITournamentUnitOfWork
    {
        IRepositoryBase<TournamentEntity> TournamentRepository { get; }
        IRepositoryBase<TournamentParticipantEntity> TournamentParticipantRepository { get; }
        IRepositoryBase<TournamentRoundEntity> TournamentRoundRepository { get; }
        IRepositoryBase<TournamentMatchEntity> TournamentMatchRepository { get; }
        IRepositoryBase<TournamentMatchLegEntity> TournamentMatchLegRepository { get; }
        IRepositoryBase<TournamentMatchVisitEntity> TournamentMatchVisitRepository { get; }
        IRepositoryBase<TournamentMatchThrowEntity> TournamentMatchThrowRepository { get; }
    }
}
