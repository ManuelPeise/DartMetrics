using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.Training
{
    public class AroundTheClockTrainingThrowEntity : AEntityBase
    {
        public int ThrowNumber { get; set; }

        public int Segment { get; set; }

        public int Multiplier { get; set; }

        public int Score { get; set; }

        public int AroundTheClockTrainingVisitId { get; set; }

        [ForeignKey(nameof(AroundTheClockTrainingVisitId))]
        public AroundTheClockTrainingVisitEntity Visit { get; set; } = null!;
    }
}
