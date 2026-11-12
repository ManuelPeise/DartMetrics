using Data.DartContext.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Training
{
    public class SegmentTrainingConfiguration : AEntityBaseConfiguration<SegmentTrainingEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<SegmentTrainingEntity> builder)
        {
            builder.ToTable("SegmentTrainings", table =>
            {
                table.HasCheckConstraint(
                    "CK_SegmentTraining_Target",
                    DartThrowConstraints.Target("TargetSegment", "TargetMultiplier"));

                table.HasCheckConstraint(
                    "CK_SegmentTraining_NumberOfVisits",
                    "`NumberOfVisits` > 0");
            });

            // 1:1 - a session has at most one segment training
            builder.HasOne(x => x.TrainingSession)
                .WithOne(x => x.SegmentTraining)
                .HasForeignKey<SegmentTrainingEntity>(x => x.TrainingSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.TrainingSessionId).IsUnique();
        }
    }
}
