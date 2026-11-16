using Data.DartContext.Entities.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Bot
{
    public class DartBotTrainingJobConfiguration : AEntityBaseConfiguration<DartBotTrainingJobEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<DartBotTrainingJobEntity> builder)
        {
            builder.ToTable("DartBotTrainingJobs", table =>
            {
                table.HasCheckConstraint("CK_DartBotTrainingJob_Progress", "`ProgressPercent` BETWEEN 0 AND 100");
            });

            builder.Property(x => x.ProgressText).HasMaxLength(500);
            builder.Property(x => x.RequestedBy).HasMaxLength(100).IsRequired();
            builder.Property(x => x.ErrorMessage).HasMaxLength(2000);

            builder.HasOne(x => x.Dataset)
                .WithMany()
                .HasForeignKey(x => x.DatasetId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Model)
                .WithMany()
                .HasForeignKey(x => x.ModelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.PolicyModel)
                .WithMany()
                .HasForeignKey(x => x.PolicyModelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.Status);

            builder.UseUtcDateTimes();
        }
    }
}
