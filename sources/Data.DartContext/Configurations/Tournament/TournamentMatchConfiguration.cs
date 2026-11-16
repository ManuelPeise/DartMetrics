using Data.DartContext.Configurations.Bot;
using Data.DartContext.Entities.Tournament;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Enums.Tournament;

namespace Data.DartContext.Configurations.Tournament
{
    public class TournamentMatchConfiguration : AEntityBaseConfiguration<TournamentMatchEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<TournamentMatchEntity> builder)
        {
            const int scheduled = (int)TournamentMatchStatusEnum.Scheduled;
            const int running = (int)TournamentMatchStatusEnum.Running;
            const int finished = (int)TournamentMatchStatusEnum.Finished;
            const int bye = (int)TournamentMatchStatusEnum.Bye;

            builder.ToTable("TournamentMatches", table =>
            {
                table.HasCheckConstraint("CK_TournamentMatch_Legs", "`HomeLegs` >= 0 AND `AwayLegs` >= 0");
                table.HasCheckConstraint(
                    "CK_TournamentMatch_Sides",
                    "`HomeParticipantId` IS NULL OR `AwayParticipantId` IS NULL OR `HomeParticipantId` <> `AwayParticipantId`");

                // Läuft oder beendet = beide Seiten stehen fest; Freilos = genau eine Seite.
                table.HasCheckConstraint(
                    "CK_TournamentMatch_Status",
                    $"(`Status` = {scheduled}) " +
                    $"OR (`Status` IN ({running}, {finished}) AND `HomeParticipantId` IS NOT NULL AND `AwayParticipantId` IS NOT NULL AND `StartedAt` IS NOT NULL) " +
                    $"OR (`Status` = {bye} AND (`HomeParticipantId` IS NULL) <> (`AwayParticipantId` IS NULL))");
                table.HasCheckConstraint(
                    "CK_TournamentMatch_Winner",
                    $"(`Status` IN ({finished}, {bye}) AND `WinnerParticipantId` IS NOT NULL) OR (`Status` IN ({scheduled}, {running}) AND `WinnerParticipantId` IS NULL)");
                table.HasCheckConstraint(
                    "CK_TournamentMatch_FinishedAt",
                    $"(`Status` = {finished} AND `FinishedAt` IS NOT NULL) OR (`Status` <> {finished} AND `FinishedAt` IS NULL)");
                table.HasCheckConstraint("CK_TournamentMatch_Xp", "(`HomeXpGained` IS NULL OR `HomeXpGained` >= 0) AND (`AwayXpGained` IS NULL OR `AwayXpGained` >= 0)");
                table.HasCheckConstraint("CK_TournamentMatch_Learning", $"`LearningProcessedAt` IS NULL OR `Status` = {finished}");
                table.HasCheckConstraint(
                    "CK_TournamentMatch_NextMatch",
                    "(`NextMatchId` IS NULL AND `NextMatchSlot` IS NULL) OR (`NextMatchId` IS NOT NULL AND `NextMatchSlot` IN (1, 2))");
            });

            builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();

            builder.HasIndex(x => new { x.TournamentRoundId, x.MatchNumber }).IsUnique();

            // Pro Zielmatch und Seite höchstens ein Vorgänger.
            builder.HasIndex(x => new { x.NextMatchId, x.NextMatchSlot }).IsUnique();

            builder.HasOne(x => x.Round)
                .WithMany(x => x.Matches)
                .HasForeignKey(x => x.TournamentRoundId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.HomeParticipant).WithMany().HasForeignKey(x => x.HomeParticipantId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.AwayParticipant).WithMany().HasForeignKey(x => x.AwayParticipantId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.WinnerParticipant).WithMany().HasForeignKey(x => x.WinnerParticipantId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.HomeProfileVersion).WithMany().HasForeignKey(x => x.HomeProfileVersionId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.AwayProfileVersion).WithMany().HasForeignKey(x => x.AwayProfileVersionId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.NextMatch).WithMany().HasForeignKey(x => x.NextMatchId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ConductedByUser).WithMany().HasForeignKey(x => x.ConductedByUserId).OnDelete(DeleteBehavior.Restrict);

            builder.UseUtcDateTimes();
        }
    }
}
