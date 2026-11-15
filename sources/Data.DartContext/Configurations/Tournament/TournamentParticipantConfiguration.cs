using Data.DartContext.Configurations.Bot;
using Data.DartContext.Entities.Tournament;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Enums.Tournament;

namespace Data.DartContext.Configurations.Tournament
{
    public class TournamentParticipantConfiguration : AEntityBaseConfiguration<TournamentParticipantEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<TournamentParticipantEntity> builder)
        {
            const int user = (int)TournamentParticipantTypeEnum.User;
            const int bot = (int)TournamentParticipantTypeEnum.Bot;

            builder.ToTable("TournamentParticipants", table =>
            {
                // User: kein Bot (UserId darf nach Anonymisierung NULL sein). Bot: Bot gesetzt, kein User.
                table.HasCheckConstraint(
                    "CK_TournamentParticipant_Type",
                    $"(`ParticipantType` = {user} AND `DartBotId` IS NULL) " +
                    $"OR (`ParticipantType` = {bot} AND `DartBotId` IS NOT NULL AND `UserId` IS NULL)");
                table.HasCheckConstraint("CK_TournamentParticipant_Seed", "`Seed` IS NULL OR `Seed` >= 1");
            });

            builder.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();

            // Jeder Benutzer bzw. Bot höchstens einmal je Turnier (NULL verletzt den Index nicht).
            builder.HasIndex(x => new { x.TournamentId, x.UserId }).IsUnique();
            builder.HasIndex(x => new { x.TournamentId, x.DartBotId }).IsUnique();
            builder.HasIndex(x => new { x.TournamentId, x.Seed }).IsUnique();

            builder.HasOne(x => x.Tournament)
                .WithMany(x => x.Participants)
                .HasForeignKey(x => x.TournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.DartBot)
                .WithMany()
                .HasForeignKey(x => x.DartBotId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.UseUtcDateTimes();
        }
    }
}
