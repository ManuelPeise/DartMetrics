using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.Training
{
    public class TrainingGameVisitEntity : AEntityBase
    {
        public int VisitNumber { get; set; }

        /// <summary>Leg im Satz, ab 1. VisitNumber läuft über alle Legs weiter.</summary>
        public int LegNumber { get; set; } = 1;

        public int ScoreBefore { get; set; }

        public int ScoreAfter { get; set; }

        public bool IsBust { get; set; }

        public int TrainingGameId { get; set; }

        [ForeignKey(nameof(TrainingGameId))]
        public TrainingGameEntity TrainingGame { get; set; } = null!;

        public ICollection<TrainingGameThrowEntity> Throws { get; set; } = [];
    }
}
