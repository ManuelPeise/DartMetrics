using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.Training
{
    public class SegmentTrainingThrowEntity: AEntityBase
    {
        public int VisitNumber { get; set; }
        public int ThrowNumber { get; set; }
        public int Segment { get; set; }
        public int Multiplier { get; set; }
        public int Score { get; set; }
        public bool IsHit { get; set; }

        public int SegmentTrainingId { get; set; }
        [ForeignKey(nameof(SegmentTrainingId))]
        public SegmentTrainingEntity SegmentTraining { get; set; } = null!;
    }
}
