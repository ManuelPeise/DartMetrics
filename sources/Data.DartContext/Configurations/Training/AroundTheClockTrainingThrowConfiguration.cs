using Data.DartContext.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Training
{
    public class AroundTheClockTrainingThrowConfiguration : AEntityBaseConfiguration<AroundTheClockTrainingThrowEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<AroundTheClockTrainingThrowEntity> builder)
        {
            builder.ToTable("AroundTheClockTrainingThrows", table =>
            {
                table.HasCheckConstraint("CK_AroundTheClockTrainingThrow_ThrowNumber", DartThrowConstraints.ThrowNumber);
                table.HasCheckConstraint("CK_AroundTheClockTrainingThrow_SegmentMultiplier", DartThrowConstraints.SegmentMultiplier);
                table.HasCheckConstraint("CK_AroundTheClockTrainingThrow_Score", DartThrowConstraints.Score);
            });

            builder.HasOne(x => x.Visit)
                .WithMany(x => x.Throws)
                .HasForeignKey(x => x.AroundTheClockTrainingVisitId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.AroundTheClockTrainingVisitId, x.ThrowNumber }).IsUnique();
        }
    }
}
