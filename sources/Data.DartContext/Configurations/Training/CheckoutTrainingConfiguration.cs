using Data.DartContext.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Training
{
    public class CheckoutTrainingConfiguration : AEntityBaseConfiguration<CheckoutTrainingEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<CheckoutTrainingEntity> builder)
        {
            // Highest possible checkout (double out) is 170.
            builder.ToTable("CheckoutTrainings", table =>
                table.HasCheckConstraint("CK_CheckoutTraining_TargetScore", "`TargetScore` BETWEEN 2 AND 170"));

            // 1:1 - a session has at most one checkout training
            builder.HasOne(x => x.TrainingSession)
                .WithOne(x => x.CheckoutTraining)
                .HasForeignKey<CheckoutTrainingEntity>(x => x.TrainingSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.TrainingSessionId).IsUnique();
        }
    }
}
