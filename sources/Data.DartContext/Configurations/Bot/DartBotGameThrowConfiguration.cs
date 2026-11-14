using Data.DartContext.Configurations.Training;
using Data.DartContext.Entities.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Bot
{
    public class DartBotGameThrowConfiguration : AEntityBaseConfiguration<DartBotGameThrowEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<DartBotGameThrowEntity> builder)
        {
            builder.ToTable("DartBotGameThrows", table =>
            {
                // Gleiche Kodierung wie die bestehenden Trainings-Würfe.
                table.HasCheckConstraint("CK_DartBotGameThrow_ThrowNumber", DartThrowConstraints.ThrowNumber);
                table.HasCheckConstraint("CK_DartBotGameThrow_SegmentMultiplier", DartThrowConstraints.SegmentMultiplier);
                table.HasCheckConstraint("CK_DartBotGameThrow_Score", DartThrowConstraints.Score);

                // Ziel: beide NULL (Mensch ohne Angabe) oder ein gültiges Feld (kein Miss).
                table.HasCheckConstraint(
                    "CK_DartBotGameThrow_Target",
                    "(`TargetSegment` IS NULL AND `TargetMultiplier` IS NULL) OR " +
                    DartThrowConstraints.Target(nameof(DartBotGameThrowEntity.TargetSegment), nameof(DartBotGameThrowEntity.TargetMultiplier)));

                table.HasCheckConstraint("CK_DartBotGameThrow_TargetXY", "(`TargetX` IS NULL) = (`TargetY` IS NULL)");
                table.HasCheckConstraint("CK_DartBotGameThrow_HitXY", "(`HitX` IS NULL) = (`HitY` IS NULL)");

                table.HasCheckConstraint("CK_DartBotGameThrow_ScoreBeforeThrow", "`ScoreBeforeThrow` BETWEEN 2 AND 501");
                table.HasCheckConstraint("CK_DartBotGameThrow_DartsLeftInVisit", "`DartsLeftInVisit` = 3 - `ThrowNumber`");
            });

            builder.HasIndex(x => new { x.DartBotGameVisitId, x.ThrowNumber }).IsUnique();

            builder.HasOne(x => x.Visit)
                .WithMany(x => x.Throws)
                .HasForeignKey(x => x.DartBotGameVisitId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.UseUtcDateTimes();
        }
    }
}
