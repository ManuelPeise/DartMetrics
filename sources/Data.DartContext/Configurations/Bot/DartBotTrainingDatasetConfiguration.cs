using Data.DartContext.Entities.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Bot
{
    public class DartBotTrainingDatasetConfiguration : AEntityBaseConfiguration<DartBotTrainingDatasetEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<DartBotTrainingDatasetEntity> builder)
        {
            builder.ToTable("DartBotTrainingDatasets", table =>
            {
                table.HasCheckConstraint("CK_DartBotTrainingDataset_RowCount", "`RowCount` >= 0");
                table.HasCheckConstraint("CK_DartBotTrainingDataset_SizeBytes", "`SizeBytes` >= 0");
                table.HasCheckConstraint(
                    "CK_DartBotTrainingDataset_GameRange",
                    "`FromGameId` IS NULL OR `ToGameId` IS NULL OR `FromGameId` <= `ToGameId`");
            });

            builder.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Sha256).HasMaxLength(64).IsFixedLength().IsRequired();

            builder.HasIndex(x => x.StorageKey).IsUnique();

            builder.HasOne(x => x.GeneratorTargetModel)
                .WithMany()
                .HasForeignKey(x => x.GeneratorTargetModelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.GeneratorThrowModel)
                .WithMany()
                .HasForeignKey(x => x.GeneratorThrowModelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.UseUtcDateTimes();
        }
    }
}
