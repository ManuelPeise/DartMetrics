using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.Training
{
    public class SegmentTrainingEntity: AEntityBase
    {
        public int TargetSegment { get; set; }
        public int TargetMultiplier { get; set; }
        public int NumberOfVisits { get; set; }

        public int TrainingSessionId { get; set; }
        [ForeignKey(nameof(TrainingSessionId))]
        public TrainingSessionEntity TrainingSession { get; set; } = null!;
        public ICollection<SegmentTrainingThrowEntity> Throws { get; set; } = [];
    }
}
