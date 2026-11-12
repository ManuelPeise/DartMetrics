

namespace Shared.Interfaces.UnitsOfWork
{
    public interface IUnitOfWork
    {
        IUserUnitOfWork UserUnitOfWork { get; }
        IDartTrainingUnitOfWork DartTrainingUnitOfWork { get; }
        Task<bool> SaveChangesAsync();
    }
}
