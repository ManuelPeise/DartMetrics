using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.Training
{
    public class TrainingGameVisitEntity : AEntityBase
    {
        public int VisitNumber { get; set; }

        public int ScoreBefore { get; set; }

        public int ScoreAfter { get; set; }

        public bool IsBust { get; set; }

        public int TrainingGameId { get; set; }

        [ForeignKey(nameof(TrainingGameId))]
        public TrainingGameEntity TrainingGame { get; set; } = null!;

        public ICollection<TrainingGameThrowEntity> Throws { get; set; } = [];
    }
}
