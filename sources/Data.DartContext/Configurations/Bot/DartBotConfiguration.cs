using Data.DartContext.Entities.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Bot
{
    public class DartBotConfiguration : AEntityBaseConfiguration<DartBotEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<DartBotEntity> builder)
        {
            builder.ToTable("DartBots", table =>
            {
                table.HasCheckConstraint("CK_DartBot_ExperiencePoints", "`ExperiencePoints` >= 0");
                table.HasCheckConstraint("CK_DartBot_DevelopmentLevel", "`DevelopmentLevel` >= 0");
                table.HasCheckConstraint("CK_DartBot_Rating", "`Rating` >= 0");
            });

            builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Alias).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Rating).HasDefaultValue(1000);

            builder.HasIndex(x => x.Alias).IsUnique();

            // Beziehungen werden auf der abhängigen Seite konfiguriert (ProfileVersion, GamePlayer).

            builder.UseUtcDateTimes();
        }
    }
}
