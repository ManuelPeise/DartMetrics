using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.Training
{
    public class AroundTheClockTrainingVisitEntity : AEntityBase
    {
        public int VisitNumber { get; set; }

        public int AroundTheClockTrainingId { get; set; }

        [ForeignKey(nameof(AroundTheClockTrainingId))]
        public AroundTheClockTrainingEntity AroundTheClockTraining { get; set; } = null!;

        public ICollection<AroundTheClockTrainingThrowEntity> Throws { get; set; } = [];
    }
}
