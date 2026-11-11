

namespace Shared.Interfaces.UnitsOfWork
{
    public interface IUnitOfWork
    {
        IUserUnitOfWork UserUnitOfWork { get; }
        Task<bool> SaveChangesAsync();
    }
}
