using Data.DartContext.Configurations.Bot;
using Data.DartContext.Entities.Tournament;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Enums.Tournament;

namespace Data.DartContext.Configurations.Tournament
{
    public class TournamentConfiguration : AEntityBaseConfiguration<TournamentEntity>
    {
        public const int NameMaxLength = 100;

        protected override void ConfigureEntity(EntityTypeBuilder<TournamentEntity> builder)
        {
            const int knockout = (int)TournamentTypeEnum.Knockout;
            const int running = (int)TournamentStatusEnum.Running;
            const int finished = (int)TournamentStatusEnum.Finished;
            const int cancelled = (int)TournamentStatusEnum.Cancelled;

            builder.ToTable("Tournaments", table =>
            {
                table.HasCheckConstraint("CK_Tournament_StartingScore", "`StartingScore` IN (301, 501)");

                // Setzliste nur im K.-o.-System; Anzahl Gesetzte eine Zweierpotenz (2, 4, 8).
                table.HasCheckConstraint(
                    "CK_Tournament_Seeding",
                    $"(`SeedSourceTournamentId` IS NULL AND `SeedCount` IS NULL) " +
                    $"OR (`Type` = {knockout} AND `SeedCount` IN (2, 4, 8))");

                table.HasCheckConstraint(
                    "CK_Tournament_StartedAt",
                    $"`Status` NOT IN ({running}, {finished}) OR `StartedAt` IS NOT NULL");
                table.HasCheckConstraint(
                    "CK_Tournament_FinishedAt",
                    $"(`Status` IN ({finished}, {cancelled}) AND `FinishedAt` IS NOT NULL) OR (`Status` NOT IN ({finished}, {cancelled}) AND `FinishedAt` IS NULL)");
                table.HasCheckConstraint("CK_Tournament_Winner", $"`WinnerParticipantId` IS NULL OR `Status` = {finished}");
            });

            builder.Property(x => x.Name).HasMaxLength(NameMaxLength).IsRequired();
            builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();

            builder.HasIndex(x => new { x.Kind, x.Status });

            builder.HasOne(x => x.OwnerUser)
                .WithMany()
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SeedSourceTournament)
                .WithMany()
                .HasForeignKey(x => x.SeedSourceTournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.WinnerParticipant)
                .WithMany()
                .HasForeignKey(x => x.WinnerParticipantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.UseUtcDateTimes();
        }
    }
}
