using Data.DartContext.Configurations.Training;
using Data.DartContext.Entities.Tournament;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Tournament
{
    public class TournamentMatchThrowConfiguration : AEntityBaseConfiguration<TournamentMatchThrowEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<TournamentMatchThrowEntity> builder)
        {
            builder.ToTable("TournamentMatchThrows", table =>
            {
                table.HasCheckConstraint("CK_TournamentMatchThrow_ThrowNumber", DartThrowConstraints.ThrowNumber);
                table.HasCheckConstraint("CK_TournamentMatchThrow_SegmentMultiplier", DartThrowConstraints.SegmentMultiplier);
                table.HasCheckConstraint("CK_TournamentMatchThrow_Score", DartThrowConstraints.Score);
                table.HasCheckConstraint(
                    "CK_TournamentMatchThrow_Target",
                    "(`TargetSegment` IS NULL AND `TargetMultiplier` IS NULL) OR " +
                    DartThrowConstraints.Target(nameof(TournamentMatchThrowEntity.TargetSegment), nameof(TournamentMatchThrowEntity.TargetMultiplier)));
            });

            builder.HasIndex(x => new { x.TournamentMatchVisitId, x.ThrowNumber }).IsUnique();

            builder.HasOne(x => x.Visit)
                .WithMany(x => x.Throws)
                .HasForeignKey(x => x.TournamentMatchVisitId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
