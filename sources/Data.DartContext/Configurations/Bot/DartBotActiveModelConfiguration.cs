using Data.DartContext.Entities.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Bot
{
    public class DartBotActiveModelConfiguration : AEntityBaseConfiguration<DartBotActiveModelEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<DartBotActiveModelEntity> builder)
        {
            builder.ToTable("DartBotActiveModels");

            builder.Property(x => x.ActivatedBy).HasMaxLength(AuditUserMaxLength);

            // Genau ein aktives Modell pro Typ (Ersatz für einen gefilterten Index, den MySQL nicht kennt).
            builder.HasIndex(x => x.ModelType).IsUnique();

            builder.HasOne(x => x.Model)
                .WithMany()
                .HasForeignKey(x => new { x.ModelType, x.ModelId })
                .HasPrincipalKey(x => new { x.ModelType, x.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.UseUtcDateTimes();
        }
    }
}
