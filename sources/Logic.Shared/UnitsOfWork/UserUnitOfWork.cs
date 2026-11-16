using Data.DartContext;
using Data.DartContext.Entities.User;
using Logic.Shared.Repositories;
using Shared.Interfaces.Repositories;
using Shared.Interfaces.UnitsOfWork;

namespace Logic.Shared.UnitsOfWork
{
    public class UserUnitOfWork : IUserUnitOfWork
    {
        private readonly DartDatabaseContext _dartDatabaseContext;
        public IRepositoryBase<UserEntity> UserRepository { get; }
        public IRepositoryBase<UserRoleEntity> UserRoleRepository { get; }
        public IRepositoryBase<UserCredentialsEntity> UserCredentialsRepository { get; }
        public IRepositoryBase<UserAvatarEntity> UserAvatarRepository { get; }

        public IRepositoryBase<UserAchievementEntity> UserAchievementRepository { get; }

        public IRepositoryBase<RoleEntity> RoleRepository { get; }

        public UserUnitOfWork(DartDatabaseContext dartDatabaseContext)
        {
            _dartDatabaseContext = dartDatabaseContext;
          
            UserRepository = new RepositoryBase<UserEntity>(_dartDatabaseContext);
            UserRoleRepository = new RepositoryBase<UserRoleEntity>(_dartDatabaseContext);
            UserCredentialsRepository = new RepositoryBase<UserCredentialsEntity>(_dartDatabaseContext);
            RoleRepository = new RepositoryBase<RoleEntity>(_dartDatabaseContext);
            UserAvatarRepository = new RepositoryBase<UserAvatarEntity>(_dartDatabaseContext);
            UserAchievementRepository = new RepositoryBase<UserAchievementEntity>(_dartDatabaseContext);
        }
    }
}
