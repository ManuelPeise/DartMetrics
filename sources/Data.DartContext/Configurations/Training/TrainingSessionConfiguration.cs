using Data.DartContext.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Training
{
    public class TrainingSessionConfiguration : AEntityBaseConfiguration<TrainingSessionEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<TrainingSessionEntity> builder)
        {
            builder.ToTable("TrainingSessions");

            builder.Property(x => x.TrainingType)
                .IsRequired();

            builder.Property(x => x.StartedAt)
                .IsRequired();

            builder.Property(x => x.Notes)
                .HasMaxLength(500);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.UserId, x.StartedAt });
        }
    }
}
