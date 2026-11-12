using Data.DartContext.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Training
{
    public class TrainingGameConfiguration : AEntityBaseConfiguration<TrainingGameEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<TrainingGameEntity> builder)
        {
            builder.ToTable("TrainingGames", table =>
                table.HasCheckConstraint("CK_TrainingGame_StartingScore", "`StartingScore` IN (301, 501)"));

            // 1:1 - a session has at most one game
            builder.HasOne(x => x.TrainingSession)
                .WithOne(x => x.TrainingGame)
                .HasForeignKey<TrainingGameEntity>(x => x.TrainingSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.TrainingSessionId).IsUnique();
        }
    }
}
