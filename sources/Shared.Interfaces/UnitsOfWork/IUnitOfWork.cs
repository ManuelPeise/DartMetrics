

namespace Shared.Interfaces.UnitsOfWork
{
    public interface IUnitOfWork
    {
        IUserUnitOfWork UserUnitOfWork { get; }
        IDartTrainingUnitOfWork DartTrainingUnitOfWork { get; }
        IDartBotUnitOfWork DartBotUnitOfWork { get; }
        ITournamentUnitOfWork TournamentUnitOfWork { get; }
        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
