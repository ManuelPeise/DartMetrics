using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.Training
{
    public class CheckoutTrainingEntity : AEntityBase
    {
        public int TargetScore { get; set; }

        public int TrainingSessionId { get; set; }

        [ForeignKey(nameof(TrainingSessionId))]
        public TrainingSessionEntity TrainingSession { get; set; } = null!;

        public ICollection<CheckoutTrainingAttemptEntity> Attempts { get; set; } = [];
    }
}
