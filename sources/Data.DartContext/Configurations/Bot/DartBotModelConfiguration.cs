using Data.DartContext.Entities.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Bot
{
    public class DartBotModelConfiguration : AEntityBaseConfiguration<DartBotModelEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<DartBotModelEntity> builder)
        {
            builder.ToTable("DartBotModels", table =>
            {
                table.HasCheckConstraint("CK_DartBotModel_VersionNumber", "`VersionNumber` >= 1");
                table.HasCheckConstraint("CK_DartBotModel_SizeBytes", "`SizeBytes` >= 0");
            });

            builder.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Sha256).HasMaxLength(64).IsFixedLength().IsRequired();
            builder.Property(x => x.MlNetVersion).HasMaxLength(50).IsRequired();

            builder.HasIndex(x => new { x.ModelType, x.VersionNumber }).IsUnique();
            builder.HasIndex(x => x.StorageKey).IsUnique();

            // Ziel für den zusammengesetzten FK aus ActiveModel: Typ muss passen.
            builder.HasAlternateKey(x => new { x.ModelType, x.Id });

            // Jedes Modell stammt aus genau einem Trainingslauf (1:0..1, TrainingId eindeutig).
            builder.HasOne(x => x.Training)
                .WithOne(x => x.ResultingModel)
                .HasForeignKey<DartBotModelEntity>(x => x.TrainingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.UseUtcDateTimes();
        }
    }
}
