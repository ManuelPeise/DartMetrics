using Data.DartContext.Configurations.Bot;
using Data.DartContext.Entities.Tournament;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Tournament
{
    public class TournamentMatchLegConfiguration : AEntityBaseConfiguration<TournamentMatchLegEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<TournamentMatchLegEntity> builder)
        {
            builder.ToTable("TournamentMatchLegs", table =>
            {
                table.HasCheckConstraint("CK_TournamentMatchLeg_LegNumber", "`LegNumber` >= 1");
                table.HasCheckConstraint("CK_TournamentMatchLeg_StartingPlayer", "`StartingPlayerNumber` IN (1, 2)");
                table.HasCheckConstraint(
                    "CK_TournamentMatchLeg_Winner",
                    "(`WinnerPlayerNumber` IS NULL AND `FinishedAt` IS NULL) OR (`WinnerPlayerNumber` IN (1, 2) AND `FinishedAt` IS NOT NULL)");
            });

            builder.HasIndex(x => new { x.TournamentMatchId, x.LegNumber }).IsUnique();

            builder.HasOne(x => x.Match)
                .WithMany(x => x.Legs)
                .HasForeignKey(x => x.TournamentMatchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.UseUtcDateTimes();
        }
    }
}
