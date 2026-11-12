using Data.DartContext.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Training
{
    public class SegmentTrainingThrowConfiguration : AEntityBaseConfiguration<SegmentTrainingThrowEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<SegmentTrainingThrowEntity> builder)
        {
            builder.ToTable("SegmentTrainingThrows", table =>
            {
                table.HasCheckConstraint("CK_SegmentTrainingThrow_VisitNumber", "`VisitNumber` > 0");
                table.HasCheckConstraint("CK_SegmentTrainingThrow_ThrowNumber", DartThrowConstraints.ThrowNumber);
                table.HasCheckConstraint("CK_SegmentTrainingThrow_SegmentMultiplier", DartThrowConstraints.SegmentMultiplier);
                table.HasCheckConstraint("CK_SegmentTrainingThrow_Score", DartThrowConstraints.Score);
            });

            builder.HasOne(x => x.SegmentTraining)
                .WithMany(x => x.Throws)
                .HasForeignKey(x => x.SegmentTrainingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.SegmentTrainingId, x.VisitNumber, x.ThrowNumber }).IsUnique();
        }
    }
}
