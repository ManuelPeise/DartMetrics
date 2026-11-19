using Data.DartContext.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Training
{
    public class Bobs27TrainingTurnConfiguration : AEntityBaseConfiguration<Bobs27TrainingTurnEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<Bobs27TrainingTurnEntity> builder)
        {
            builder.ToTable("Bobs27TrainingTurns", table =>
            {
                table.HasCheckConstraint("CK_Bobs27TrainingTurn_TurnNumber", "`TurnNumber` > 0");
                table.HasCheckConstraint("CK_Bobs27TrainingTurn_PlayerNumber", $"`PlayerNumber` BETWEEN 1 AND {Bobs27TrainingPlayerConfiguration.MaxPlayers}");
                table.HasCheckConstraint("CK_Bobs27TrainingTurn_Round", "`Round` BETWEEN 1 AND 21");
                table.HasCheckConstraint("CK_Bobs27TrainingTurn_Hits", "`Hits` BETWEEN 0 AND 3");

                // Wertung: Treffer × Zielwert dazu, ohne Treffer Zielwert ab (D1 … D20 = 2 … 40, Runde 21 = Bull 50).
                table.HasCheckConstraint(
                    "CK_Bobs27TrainingTurn_Score",
                    "`ScoreAfter` = `ScoreBefore` + IF(`Hits` > 0, `Hits`, -1) * IF(`Round` = 21, 50, `Round` * 2)");
            });

            builder.HasOne(x => x.Bobs27Training)
                .WithMany(x => x.Turns)
                .HasForeignKey(x => x.Bobs27TrainingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Jeder Zug nur einmal; jeder Spieler wirft jede Runde höchstens einmal.
            builder.HasIndex(x => new { x.Bobs27TrainingId, x.TurnNumber }).IsUnique();
            builder.HasIndex(x => new { x.Bobs27TrainingId, x.PlayerNumber, x.Round }).IsUnique();
        }
    }
}
