using Data.DartContext.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Training
{
    public class AroundTheClockTrainingConfiguration : AEntityBaseConfiguration<AroundTheClockTrainingEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<AroundTheClockTrainingEntity> builder)
        {
            builder.ToTable("AroundTheClockTrainings");

            // 1:1 - eine Session hat höchstens ein Around-the-Clock-Training
            builder.HasOne(x => x.TrainingSession)
                .WithOne(x => x.AroundTheClockTraining)
                .HasForeignKey<AroundTheClockTrainingEntity>(x => x.TrainingSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.TrainingSessionId).IsUnique();
        }
    }
}
