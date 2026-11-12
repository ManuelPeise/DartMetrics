using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.Training
{
    public class TrainingGameThrowEntity : AEntityBase
    {
        public int ThrowNumber { get; set; }

        public int Segment { get; set; }

        public int Multiplier { get; set; }

        public int Score { get; set; }

        public int TrainingGameVisitId { get; set; }

        [ForeignKey(nameof(TrainingGameVisitId))]
        public TrainingGameVisitEntity Visit { get; set; } = null!;
    }
}
