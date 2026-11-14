using Data.DartContext.Entities.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Enums.Bot;

namespace Data.DartContext.Configurations.Bot
{
    public class DartBotModelTrainingConfiguration : AEntityBaseConfiguration<DartBotModelTrainingEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<DartBotModelTrainingEntity> builder)
        {
            const int completed = (int)DartBotModelTrainingStatusEnum.Completed;
            const int failed = (int)DartBotModelTrainingStatusEnum.Failed;

            builder.ToTable("DartBotModelTrainings", table =>
            {
                table.HasCheckConstraint("CK_DartBotModelTraining_TrainingDataCount", "`TrainingDataCount` >= 0");
                table.HasCheckConstraint(
                    "CK_DartBotModelTraining_FinishedAt",
                    $"`Status` NOT IN ({completed}, {failed}) OR `FinishedAt` IS NOT NULL");
            });

            builder.Property(x => x.StartedAt).IsRequired();
            builder.Property(x => x.TrainerName).HasMaxLength(200);
            builder.Property(x => x.MlNetVersion).HasMaxLength(50);
            builder.Property(x => x.CodeVersion).HasMaxLength(64);

            builder.HasOne(x => x.Dataset)
                .WithMany(x => x.Trainings)
                .HasForeignKey(x => x.DatasetId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.ModelType, x.Status });

            builder.UseUtcDateTimes();
        }
    }
}
