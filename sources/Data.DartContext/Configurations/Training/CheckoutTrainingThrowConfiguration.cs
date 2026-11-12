using Data.DartContext.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Training
{
    public class CheckoutTrainingThrowConfiguration : AEntityBaseConfiguration<CheckoutTrainingThrowEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<CheckoutTrainingThrowEntity> builder)
        {
            builder.ToTable("CheckoutTrainingThrows", table =>
            {
                table.HasCheckConstraint("CK_CheckoutTrainingThrow_ThrowNumber", DartThrowConstraints.ThrowNumber);
                table.HasCheckConstraint("CK_CheckoutTrainingThrow_SegmentMultiplier", DartThrowConstraints.SegmentMultiplier);
                table.HasCheckConstraint("CK_CheckoutTrainingThrow_Score", DartThrowConstraints.Score);
            });

            builder.HasOne(x => x.Attempt)
                .WithMany(x => x.Throws)
                .HasForeignKey(x => x.CheckoutTrainingAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.CheckoutTrainingAttemptId, x.ThrowNumber }).IsUnique();
        }
    }
}
