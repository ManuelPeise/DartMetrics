using Data.DartContext.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Training
{
    public class TrainingGameVisitConfiguration : AEntityBaseConfiguration<TrainingGameVisitEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<TrainingGameVisitEntity> builder)
        {
            builder.ToTable("TrainingGameVisits", table =>
            {
                table.HasCheckConstraint("CK_TrainingGameVisit_VisitNumber", "`VisitNumber` > 0");
                table.HasCheckConstraint("CK_TrainingGameVisit_ScoreBefore", "`ScoreBefore` BETWEEN 2 AND 501");

                // A visit never increases the rest score; max. 180 per visit; a bust keeps the score.
                table.HasCheckConstraint(
                    "CK_TrainingGameVisit_ScoreAfter",
                    "`ScoreAfter` BETWEEN 0 AND `ScoreBefore` " +
                    "AND `ScoreBefore` - `ScoreAfter` <= 180 " +
                    "AND (`IsBust` = 0 OR `ScoreAfter` = `ScoreBefore`)");
            });

            builder.HasOne(x => x.TrainingGame)
                .WithMany(x => x.Visits)
                .HasForeignKey(x => x.TrainingGameId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.TrainingGameId, x.VisitNumber }).IsUnique();
        }
    }
}
