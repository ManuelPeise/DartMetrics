using Data.DartContext.Entities.Training;
using Shared.Enums.X01;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Training
{
    public class TrainingGameConfiguration : AEntityBaseConfiguration<TrainingGameEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<TrainingGameEntity> builder)
        {
            builder.ToTable("TrainingGames", table =>
            {
                table.HasCheckConstraint("CK_TrainingGame_StartingScore", "`StartingScore` IN (301, 501)");
                table.HasCheckConstraint("CK_TrainingGame_NumberOfLegs", "`NumberOfLegs` >= 1");
            });

            // Bestehende Trainings waren ein einzelnes Leg.
            builder.Property(x => x.NumberOfLegs).HasDefaultValue(1);

            // Bestehende Spiele: Straight-In / Double-Out.
            builder.Property(x => x.InMode).HasDefaultValue(X01InModeEnum.Straight).HasSentinel((X01InModeEnum)0);
            builder.Property(x => x.OutMode).HasDefaultValue(X01OutModeEnum.Double).HasSentinel((X01OutModeEnum)0);

            // 1:1 - a session has at most one game
            builder.HasOne(x => x.TrainingSession)
                .WithOne(x => x.TrainingGame)
                .HasForeignKey<TrainingGameEntity>(x => x.TrainingSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.TrainingSessionId).IsUnique();
        }
    }
}
