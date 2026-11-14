using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.Training
{
    public class AroundTheClockTrainingEntity : AEntityBase
    {
        public int TrainingSessionId { get; set; }

        [ForeignKey(nameof(TrainingSessionId))]
        public TrainingSessionEntity TrainingSession { get; set; } = null!;

        public ICollection<AroundTheClockTrainingVisitEntity> Visits { get; set; } = [];
    }
}
