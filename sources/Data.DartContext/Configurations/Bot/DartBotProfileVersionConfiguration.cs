using Data.DartContext.Entities.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Enums.Bot;

namespace Data.DartContext.Configurations.Bot
{
    public class DartBotProfileVersionConfiguration : AEntityBaseConfiguration<DartBotProfileVersionEntity>
    {
        private static string Skill(string column) => $"`{column}` BETWEEN 0 AND 1";

        protected override void ConfigureEntity(EntityTypeBuilder<DartBotProfileVersionEntity> builder)
        {
            builder.ToTable("DartBotProfileVersions", table =>
            {
                table.HasCheckConstraint("CK_DartBotProfileVersion_VersionNumber", "`VersionNumber` >= 1");
                table.HasCheckConstraint("CK_DartBotProfileVersion_ThrowPrecision", Skill(nameof(DartBotProfileVersionEntity.ThrowPrecision)));
                table.HasCheckConstraint("CK_DartBotProfileVersion_ThrowConsistency", Skill(nameof(DartBotProfileVersionEntity.ThrowConsistency)));
                table.HasCheckConstraint("CK_DartBotProfileVersion_TacticalIntelligence", Skill(nameof(DartBotProfileVersionEntity.TacticalIntelligence)));
                table.HasCheckConstraint("CK_DartBotProfileVersion_PressureResistance", Skill(nameof(DartBotProfileVersionEntity.PressureResistance)));
                table.HasCheckConstraint("CK_DartBotProfileVersion_RiskTaking", Skill(nameof(DartBotProfileVersionEntity.RiskTaking)));
                table.HasCheckConstraint("CK_DartBotProfileVersion_Nervousness", Skill(nameof(DartBotProfileVersionEntity.Nervousness)));
                table.HasCheckConstraint("CK_DartBotProfileVersion_BadDartRate", Skill(nameof(DartBotProfileVersionEntity.BadDartRate)));
                table.HasCheckConstraint("CK_DartBotProfileVersion_SigmaMm", "`SigmaMm` IS NULL OR `SigmaMm` > 0");
                table.HasCheckConstraint("CK_DartBotProfileVersion_VerticalRatio", "`VerticalRatio` > 0");

                // Lernen aus einem Spiel braucht das auslösende Spiel.
                table.HasCheckConstraint(
                    "CK_DartBotProfileVersion_GameLearning",
                    $"`ChangeReason` <> {(int)DartBotProfileChangeReasonEnum.GameLearning} OR `TriggeringGameId` IS NOT NULL");
            });

            builder.Property(x => x.ReasonText).HasMaxLength(1000);

            // Aktuelle Version = höchste VersionNumber; der Unique Index fängt parallele Versionen ab.
            builder.HasIndex(x => new { x.DartBotId, x.VersionNumber }).IsUnique();

            // Ziel für den zusammengesetzten FK aus GamePlayer: Version gehört garantiert zum Bot.
            builder.HasAlternateKey(x => new { x.DartBotId, x.Id });

            builder.HasOne(x => x.DartBot)
                .WithMany(x => x.ProfileVersions)
                .HasForeignKey(x => x.DartBotId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.PreviousVersion)
                .WithMany()
                .HasForeignKey(x => x.PreviousVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.TriggeringGame)
                .WithMany()
                .HasForeignKey(x => x.TriggeringGameId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.UseUtcDateTimes();
        }
    }
}
