using Data.DartContext.Entities.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Bot
{
    public class DartBotGameVisitConfiguration : AEntityBaseConfiguration<DartBotGameVisitEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<DartBotGameVisitEntity> builder)
        {
            builder.ToTable("DartBotGameVisits", table =>
            {
                table.HasCheckConstraint("CK_DartBotGameVisit_TurnNumber", "`TurnNumber` >= 1");
                table.HasCheckConstraint("CK_DartBotGameVisit_VisitNumber", "`VisitNumber` >= 1");
                table.HasCheckConstraint("CK_DartBotGameVisit_ScoreBefore", "`ScoreBefore` BETWEEN 1 AND 501");
                table.HasCheckConstraint("CK_DartBotGameVisit_OpponentScoreBefore", "`OpponentScoreBefore` BETWEEN 1 AND 501");
                table.HasCheckConstraint("CK_DartBotGameVisit_DartsUsed", "`DartsUsed` BETWEEN 0 AND 3");

                // Wie TrainingGameVisits: Rest sinkt, max. 180, Bust behält den Stand.
                table.HasCheckConstraint(
                    "CK_DartBotGameVisit_ScoreAfter",
                    "`ScoreAfter` BETWEEN 0 AND `ScoreBefore` " +
                    "AND `ScoreBefore` - `ScoreAfter` <= 180 " +
                    "AND (`IsBust` = 0 OR `ScoreAfter` = `ScoreBefore`)");

                table.HasCheckConstraint("CK_DartBotGameVisit_BustCheckout", "NOT (`IsBust` = 1 AND `IsCheckout` = 1)");
                table.HasCheckConstraint(
                    "CK_DartBotGameVisit_Checkout",
                    "(`IsCheckout` = 1 AND `ScoreAfter` = 0 AND `DartsUsed` >= 1) OR (`IsCheckout` = 0 AND `ScoreAfter` > 0)");
            });

            builder.HasIndex(x => new { x.DartBotGameLegId, x.TurnNumber }).IsUnique();
            builder.HasIndex(x => new { x.DartBotGameLegId, x.DartBotGamePlayerId, x.VisitNumber }).IsUnique();

            // Leg und Spieler müssen zum selben Spiel gehören (über das denormalisierte DartBotGameId).
            builder.HasOne(x => x.Leg)
                .WithMany(x => x.Visits)
                .HasForeignKey(x => new { x.DartBotGameId, x.DartBotGameLegId })
                .HasPrincipalKey(x => new { x.DartBotGameId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Player)
                .WithMany(x => x.Visits)
                .HasForeignKey(x => new { x.DartBotGameId, x.DartBotGamePlayerId })
                .HasPrincipalKey(x => new { x.DartBotGameId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.UseUtcDateTimes();
        }
    }
}
