using Data.DartContext.Entities.Log;
using Data.DartContext.Entities.Training;
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

        // Training
        public DbSet<TrainingSessionEntity> TrainingSessions => Set<TrainingSessionEntity>();
        
        // Segment training
        public DbSet<SegmentTrainingEntity> SegmentTrainings => Set<SegmentTrainingEntity>();
        public DbSet<SegmentTrainingThrowEntity> SegmentTrainingThrows => Set<SegmentTrainingThrowEntity>();

        // Checkout training
        public DbSet<CheckoutTrainingEntity> CheckoutTrainings => Set<CheckoutTrainingEntity>();
        public DbSet<CheckoutTrainingAttemptEntity> CheckoutTrainingAttempts => Set<CheckoutTrainingAttemptEntity>();
        public DbSet<CheckoutTrainingThrowEntity> CheckoutTrainingThrows => Set<CheckoutTrainingThrowEntity>();

        // 301 / 501 training games
        public DbSet<TrainingGameEntity> TrainingGames => Set<TrainingGameEntity>();
        public DbSet<TrainingGameVisitEntity> TrainingGameVisits => Set<TrainingGameVisitEntity>();
        public DbSet<TrainingGameThrowEntity> TrainingGameThrows => Set<TrainingGameThrowEntity>();
        

        public DartDatabaseContext(DbContextOptions options): base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Picks up ALL IEntityTypeConfiguration classes of this project (User, Log, Training, Seeds).
            // Do not call it a second time for the same assembly: every configuration would run twice
            // and HasData seeds (RoleSeed) fail with "another seed entity with the same key value".
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DartDatabaseContext).Assembly);
        }
    }
}
