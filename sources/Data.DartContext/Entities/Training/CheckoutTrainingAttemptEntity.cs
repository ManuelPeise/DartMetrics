using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.Training
{
    public class CheckoutTrainingAttemptEntity : AEntityBase
    {
        public int AttemptNumber { get; set; }

        public bool IsCheckout { get; set; }

        public int CheckoutTrainingId { get; set; }

        [ForeignKey(nameof(CheckoutTrainingId))]
        public CheckoutTrainingEntity CheckoutTraining { get; set; } = null!;

        public ICollection<CheckoutTrainingThrowEntity> Throws { get; set; } = [];
    }
}
