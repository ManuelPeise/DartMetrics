using Shared.Models.Shared;

namespace Shared.Models.Training
{
    /// <summary>Ergebnis eines Darts im Checkout-Training (Double-Out).</summary>
    public class CheckoutTrainingThrowResult
    {
        /// <summary>Rest des aktuellen Versuchs nach diesem Dart (bei Bust der Zielwert).</summary>
        public int RemainingScore { get; set; }

        public int RemainingDarts { get; set; }

        public bool IsBust { get; set; }

        public bool IsCheckout { get; set; }

        /// <summary>Versuch beendet (Checkout, Bust oder dritter Dart) – nächster Dart beginnt einen neuen Versuch.</summary>
        public bool IsAttemptFinished { get; set; }

        public int TotalAttempts { get; set; }

        public int SuccessfulAttempts { get; set; }

        public List<CheckoutSuggestion> CheckoutSuggestions { get; set; } = [];
    }
}
