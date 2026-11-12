using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.Training
{
    public class CheckoutTrainingThrowEntity : AEntityBase
    {
        public int ThrowNumber { get; set; }

        public int Segment { get; set; }

        public int Multiplier { get; set; }

        public int Score { get; set; }

        public int CheckoutTrainingAttemptId { get; set; }

        [ForeignKey(nameof(CheckoutTrainingAttemptId))]
        public CheckoutTrainingAttemptEntity Attempt { get; set; } = null!;
    }
}
