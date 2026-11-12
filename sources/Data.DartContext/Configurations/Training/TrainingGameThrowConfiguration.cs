using Data.DartContext.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Training
{
    public class TrainingGameThrowConfiguration : AEntityBaseConfiguration<TrainingGameThrowEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<TrainingGameThrowEntity> builder)
        {
            builder.ToTable("TrainingGameThrows", table =>
            {
                table.HasCheckConstraint("CK_TrainingGameThrow_ThrowNumber", DartThrowConstraints.ThrowNumber);
                table.HasCheckConstraint("CK_TrainingGameThrow_SegmentMultiplier", DartThrowConstraints.SegmentMultiplier);
                table.HasCheckConstraint("CK_TrainingGameThrow_Score", DartThrowConstraints.Score);
            });

            builder.HasOne(x => x.Visit)
                .WithMany(x => x.Throws)
                .HasForeignKey(x => x.TrainingGameVisitId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.TrainingGameVisitId, x.ThrowNumber }).IsUnique();
        }
    }
}
