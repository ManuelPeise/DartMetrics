using Data.DartContext.Entities.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Enums.Bot;

namespace Data.DartContext.Configurations.Bot
{
    public class DartBotGamePlayerConfiguration : AEntityBaseConfiguration<DartBotGamePlayerEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<DartBotGamePlayerEntity> builder)
        {
            const int user = (int)DartBotPlayerTypeEnum.User;
            const int bot = (int)DartBotPlayerTypeEnum.Bot;

            builder.ToTable("DartBotGamePlayers", table =>
            {
                table.HasCheckConstraint("CK_DartBotGamePlayer_PlayerNumber", "`PlayerNumber` IN (1, 2)");

                // Bot: Bot + Profilversion gesetzt, kein User.
                // User: keine Bot-Felder (Profil, XP); UserId darf NULL sein (anonymisiert).
                // Elo-Felder (RatingBefore/After, LearningProcessedAt) sind seit dem Spieler-Elo auch für User erlaubt.
                table.HasCheckConstraint(
                    "CK_DartBotGamePlayer_PlayerType",
                    $"(`PlayerType` = {bot} AND `DartBotId` IS NOT NULL AND `ProfileVersionId` IS NOT NULL AND `UserId` IS NULL) " +
                    $"OR (`PlayerType` = {user} AND `DartBotId` IS NULL AND `ProfileVersionId` IS NULL " +
                    "AND `XpGained` IS NULL)");

                table.HasCheckConstraint("CK_DartBotGamePlayer_XpGained", "`XpGained` IS NULL OR `XpGained` >= 0");
            });

            builder.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();

            builder.HasIndex(x => new { x.DartBotGameId, x.PlayerNumber }).IsUnique();

            // Ein Bot höchstens einmal pro Spiel (NULL bei Usern verletzt den Index nicht).
            builder.HasIndex(x => new { x.DartBotGameId, x.DartBotId }).IsUnique();

            // Ziel für die zusammengesetzten FKs aus Leg und Visit: Spieler gehört garantiert zum Spiel.
            builder.HasAlternateKey(x => new { x.DartBotGameId, x.Id });

            builder.HasOne(x => x.Game)
                .WithMany(x => x.Players)
                .HasForeignKey(x => x.DartBotGameId)
                .OnDelete(DeleteBehavior.Restrict);

            // Restrict statt SetNull: User werden vor dem Löschen durch die Anwendung anonymisiert.
            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.DartBot)
                .WithMany(x => x.GamePlayers)
                .HasForeignKey(x => x.DartBotId)
                .OnDelete(DeleteBehavior.Restrict);

            // Profilversion muss zum Bot gehören.
            builder.HasOne(x => x.ProfileVersion)
                .WithMany(x => x.GamePlayers)
                .HasForeignKey(x => new { x.DartBotId, x.ProfileVersionId })
                .HasPrincipalKey(x => new { x.DartBotId, x.Id })
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.UseUtcDateTimes();
        }
    }
}
