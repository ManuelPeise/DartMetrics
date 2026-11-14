using Shared.Models.Shared;

namespace Shared.Models.Training
{
    /// <summary>NextVisitNumber ist hier die Nummer des Versuchs.</summary>
    public class RunningCheckoutTraining : RunningTrainingBase
    {
        public int TargetScore { get; set; }

        public int RemainingScore { get; set; }

        public int SuccessfulAttempts { get; set; }

        public List<CheckoutSuggestion> CheckoutSuggestions { get; set; } = [];
    }
}
