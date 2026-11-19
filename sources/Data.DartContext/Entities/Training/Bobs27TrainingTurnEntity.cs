using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.Training
{
    /// <summary>Ein Zug: Treffer (0–3) eines Spielers auf das Ziel seiner Runde, mit Stand davor und danach.</summary>
    public class Bobs27TrainingTurnEntity : AEntityBase
    {
        /// <summary>Laufende Nummer im Spiel (1-basiert), bestimmt die Reihenfolge.</summary>
        public int TurnNumber { get; set; }

        public int PlayerNumber { get; set; }

        /// <summary>Runde 1–21 (Ziel D1 … D20, Bull).</summary>
        public int Round { get; set; }

        public int Hits { get; set; }

        public int ScoreBefore { get; set; }

        public int ScoreAfter { get; set; }

        public bool IsBust { get; set; }

        public int Bobs27TrainingId { get; set; }

        [ForeignKey(nameof(Bobs27TrainingId))]
        public Bobs27TrainingEntity Bobs27Training { get; set; } = null!;
    }
}
