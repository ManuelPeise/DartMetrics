using Data.DartContext.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Training
{
    public class CheckoutTrainingAttemptConfiguration : AEntityBaseConfiguration<CheckoutTrainingAttemptEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<CheckoutTrainingAttemptEntity> builder)
        {
            builder.ToTable("CheckoutTrainingAttempts", table =>
                table.HasCheckConstraint("CK_CheckoutTrainingAttempt_AttemptNumber", "`AttemptNumber` > 0"));

            builder.HasOne(x => x.CheckoutTraining)
                .WithMany(x => x.Attempts)
                .HasForeignKey(x => x.CheckoutTrainingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.CheckoutTrainingId, x.AttemptNumber }).IsUnique();
        }
    }
}
