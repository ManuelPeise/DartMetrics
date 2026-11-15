using Data.DartContext.Configurations.Bot;
using Data.DartContext.Entities.Tournament;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Tournament
{
    public class TournamentRoundConfiguration : AEntityBaseConfiguration<TournamentRoundEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<TournamentRoundEntity> builder)
        {
            builder.ToTable("TournamentRounds", table =>
            {
                table.HasCheckConstraint("CK_TournamentRound_RoundNumber", "`RoundNumber` >= 1");
                table.HasCheckConstraint("CK_TournamentRound_BestOfLegs", "`BestOfLegs` IN (5, 7, 9, 13)");
            });

            builder.Property(x => x.Name).HasMaxLength(50).IsRequired();

            builder.HasIndex(x => new { x.TournamentId, x.RoundNumber }).IsUnique();

            // Hintergrunddienst sucht fällige Runden.
            builder.HasIndex(x => x.ScheduledAt);

            builder.HasOne(x => x.Tournament)
                .WithMany(x => x.Rounds)
                .HasForeignKey(x => x.TournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.UseUtcDateTimes();
        }
    }
}
