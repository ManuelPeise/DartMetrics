using Data.DartContext.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Training
{
    public class AroundTheClockTrainingVisitConfiguration : AEntityBaseConfiguration<AroundTheClockTrainingVisitEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<AroundTheClockTrainingVisitEntity> builder)
        {
            builder.ToTable("AroundTheClockTrainingVisits", table =>
                table.HasCheckConstraint("CK_AroundTheClockTrainingVisit_VisitNumber", "`VisitNumber` > 0"));

            builder.HasOne(x => x.AroundTheClockTraining)
                .WithMany(x => x.Visits)
                .HasForeignKey(x => x.AroundTheClockTrainingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Jede Aufnahme nur einmal (schützt vor doppelten Würfen bei gleichzeitigen Anfragen).
            builder.HasIndex(x => new { x.AroundTheClockTrainingId, x.VisitNumber }).IsUnique();
        }
    }
}
