using Data.DartContext.Entities.Log;
using Data.DartContext.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace Data.DartContext
{
    public class DartDatabaseContext: DbContext
    {
        public DbSet<LogMessageEntity> LogMessages { get; set; }
        public DbSet<UserEntity> Users => Set<UserEntity>();
        public DbSet<UserCredentialsEntity> UserCredentials => Set<UserCredentialsEntity>();
        public DbSet<RoleEntity> Roles => Set<RoleEntity>();
        public DbSet<UserRoleEntity> UserRoles => Set<UserRoleEntity>();


        public DartDatabaseContext(DbContextOptions options): base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DartDatabaseContext).Assembly);
        }
    }
}
