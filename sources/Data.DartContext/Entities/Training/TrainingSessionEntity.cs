using Data.DartContext.Entities.User;
using Shared.Enums.Training;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.Training
{
    public class TrainingSessionEntity: AEntityBase
    {
        public TrainingTypeEnum TrainingType { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }

        public string? Notes { get; set; }

        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public UserEntity User { get; set; } = null!;

        public SegmentTrainingEntity? SegmentTraining { get; set; }

        public CheckoutTrainingEntity? CheckoutTraining { get; set; }
        public AroundTheClockTrainingEntity? AroundTheClockTraining { get; set; }

        public TrainingGameEntity? TrainingGame { get; set; }

        public Bobs27TrainingEntity? Bobs27Training { get; set; }
    }
}
