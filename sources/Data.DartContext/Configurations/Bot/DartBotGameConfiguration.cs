using Data.DartContext.Entities.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Enums.Bot;

namespace Data.DartContext.Configurations.Bot
{
    public class DartBotGameConfiguration : AEntityBaseConfiguration<DartBotGameEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<DartBotGameEntity> builder)
        {
            const int running = (int)DartBotGameStatusEnum.Running;
            const int finished = (int)DartBotGameStatusEnum.Finished;

            builder.ToTable("DartBotGames", table =>
            {
                table.HasCheckConstraint("CK_DartBotGame_StartingScore", "`StartingScore` IN (301, 501)");
                table.HasCheckConstraint("CK_DartBotGame_BestOfLegs", "`BestOfLegs` > 0 AND `BestOfLegs` % 2 = 1");

                // Laufend = ohne Ende; beendet oder abgebrochen = mit Endzeit.
                table.HasCheckConstraint(
                    "CK_DartBotGame_StatusFinishedAt",
                    $"(`Status` = {running} AND `FinishedAt` IS NULL) OR (`Status` <> {running} AND `FinishedAt` IS NOT NULL)");
                table.HasCheckConstraint("CK_DartBotGame_FinishedAt", "`FinishedAt` IS NULL OR `FinishedAt` >= `StartedAt`");
                table.HasCheckConstraint("CK_DartBotGame_Winner", $"`WinnerPlayerId` IS NULL OR `Status` = {finished}");
            });

            builder.Property(x => x.StartedAt).IsRequired();

            // Gewinner: einfacher FK; dass er zu diesem Spiel gehört, prüft die Anwendung
            // (ein zusammengesetzter FK über den eigenen Primärschlüssel ist mit EF nicht sauber abbildbar).
            builder.HasOne(x => x.WinnerPlayer)
                .WithMany()
                .HasForeignKey(x => x.WinnerPlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Export der Trainingsdaten.
            builder.HasIndex(x => new { x.GameMode, x.Status, x.FinishedAt });

            builder.UseUtcDateTimes();
        }
    }
}
