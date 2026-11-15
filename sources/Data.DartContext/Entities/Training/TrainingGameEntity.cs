using Shared.Enums.X01;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.Training
{
    public class TrainingGameEntity : AEntityBase
    {
        public int StartingScore { get; set; }

        public X01InModeEnum InMode { get; set; } = X01InModeEnum.Straight;

        public X01OutModeEnum OutMode { get; set; } = X01OutModeEnum.Double;

        /// <summary>Legs im Satz (Training allein: alle werden gespielt). Bestandsdaten: 1.</summary>
        public int NumberOfLegs { get; set; } = 1;

        public int TrainingSessionId { get; set; }

        [ForeignKey(nameof(TrainingSessionId))]
        public TrainingSessionEntity TrainingSession { get; set; } = null!;

        public ICollection<TrainingGameVisitEntity> Visits { get; set; } = [];
    }
}
