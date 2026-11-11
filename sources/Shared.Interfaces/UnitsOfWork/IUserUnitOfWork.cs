using Data.DartContext.Entities.User;
using Shared.Interfaces.Repositories;

namespace Shared.Interfaces.UnitsOfWork
{
    public interface IUserUnitOfWork
    {
        IRepositoryBase<UserEntity> UserRepository { get; }
        IRepositoryBase<UserCredentialsEntity> UserCredentialsRepository { get; }
        IRepositoryBase<UserRoleEntity> UserRoleRepository { get; }
        IRepositoryBase<RoleEntity> RoleRepository { get; }

    }
}
