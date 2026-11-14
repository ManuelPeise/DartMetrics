using Data.DartContext.Entities.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Bot
{
    public class DartBotGameLegConfiguration : AEntityBaseConfiguration<DartBotGameLegEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<DartBotGameLegEntity> builder)
        {
            builder.ToTable("DartBotGameLegs", table =>
            {
                table.HasCheckConstraint("CK_DartBotGameLeg_LegNumber", "`LegNumber` >= 1");
                table.HasCheckConstraint("CK_DartBotGameLeg_Winner", "`WinnerPlayerId` IS NULL OR `FinishedAt` IS NOT NULL");
                table.HasCheckConstraint("CK_DartBotGameLeg_FinishedAt", "`FinishedAt` IS NULL OR `FinishedAt` >= `StartedAt`");
            });

            builder.Property(x => x.StartedAt).IsRequired();

            builder.HasIndex(x => new { x.DartBotGameId, x.LegNumber }).IsUnique();

            // Ziel für den zusammengesetzten FK aus Visit: Leg gehört garantiert zum Spiel.
            builder.HasAlternateKey(x => new { x.DartBotGameId, x.Id });

            builder.HasOne(x => x.Game)
                .WithMany(x => x.Legs)
                .HasForeignKey(x => x.DartBotGameId)
                .OnDelete(DeleteBehavior.Restrict);

            // Start- und Siegerspieler müssen Teilnehmer desselben Spiels sein.
            builder.HasOne(x => x.StartingPlayer)
                .WithMany()
                .HasForeignKey(x => new { x.DartBotGameId, x.StartingPlayerId })
                .HasPrincipalKey(x => new { x.DartBotGameId, x.Id })
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.WinnerPlayer)
                .WithMany()
                .HasForeignKey(x => new { x.DartBotGameId, x.WinnerPlayerId })
                .HasPrincipalKey(x => new { x.DartBotGameId, x.Id })
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.UseUtcDateTimes();
        }
    }
}
