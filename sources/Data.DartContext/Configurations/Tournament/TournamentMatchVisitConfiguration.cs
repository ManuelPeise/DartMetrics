using Data.DartContext.Entities.Tournament;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Tournament
{
    public class TournamentMatchVisitConfiguration : AEntityBaseConfiguration<TournamentMatchVisitEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<TournamentMatchVisitEntity> builder)
        {
            builder.ToTable("TournamentMatchVisits", table =>
            {
                table.HasCheckConstraint("CK_TournamentMatchVisit_PlayerNumber", "`PlayerNumber` IN (1, 2)");
                table.HasCheckConstraint("CK_TournamentMatchVisit_DartsUsed", "`DartsUsed` BETWEEN 0 AND 3");
                table.HasCheckConstraint("CK_TournamentMatchVisit_VisitNumber", "`VisitNumber` >= 1");
                table.HasCheckConstraint("CK_TournamentMatchVisit_ScoreBefore", "`ScoreBefore` BETWEEN 1 AND 501");

                // Wie DartBotGameVisits: Rest sinkt, max. 180, Bust behält den Stand.
                table.HasCheckConstraint(
                    "CK_TournamentMatchVisit_ScoreAfter",
                    "`ScoreAfter` BETWEEN 0 AND `ScoreBefore` " +
                    "AND `ScoreBefore` - `ScoreAfter` <= 180 " +
                    "AND (`IsBust` = 0 OR `ScoreAfter` = `ScoreBefore`)");
                table.HasCheckConstraint("CK_TournamentMatchVisit_BustCheckout", "NOT (`IsBust` = 1 AND `IsCheckout` = 1)");
                table.HasCheckConstraint(
                    "CK_TournamentMatchVisit_Checkout",
                    "(`IsCheckout` = 1 AND `ScoreAfter` = 0 AND `DartsUsed` >= 1) OR (`IsCheckout` = 0 AND `ScoreAfter` > 0)");
            });

            builder.HasIndex(x => new { x.TournamentMatchLegId, x.VisitNumber }).IsUnique();

            builder.HasOne(x => x.Leg)
                .WithMany(x => x.Visits)
                .HasForeignKey(x => x.TournamentMatchLegId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
