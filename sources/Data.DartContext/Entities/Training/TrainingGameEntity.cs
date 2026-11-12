using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.Training
{
    public class TrainingGameEntity : AEntityBase
    {
        public int StartingScore { get; set; }

        public int TrainingSessionId { get; set; }

        [ForeignKey(nameof(TrainingSessionId))]
        public TrainingSessionEntity TrainingSession { get; set; } = null!;

        public ICollection<TrainingGameVisitEntity> Visits { get; set; } = [];
    }
}
