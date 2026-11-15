using Shared.Models.Shared;

namespace Shared.Models.Training
{
    public class X01TrainingThrowResult
    {
        public int RemainingScore { get; set; }
        public int RemainingDarts { get; set; }
        public bool IsBust { get; set; }
        /// <summary>Das Leg ist gecheckt (RemainingScore 0). Der Satz geht weiter, solange IsSetFinished false ist.</summary>
        public bool IsCheckout { get; set; }

        /// <summary>Leg dieses Darts (ab 1).</summary>
        public int LegNumber { get; set; } = 1;

        public int LegsCompleted { get; set; }

        public int NumberOfLegs { get; set; } = 1;

        /// <summary>Letztes Leg des Satzes gecheckt: das Training ist beendet.</summary>
        public bool IsSetFinished { get; set; }

        /// <summary>Bei Double-In: ist das Leg nach diesem Dart eröffnet?</summary>
        public bool IsOpened { get; set; } = true;

        public List<CheckoutSuggestion> CheckoutSuggestions { get; set; } = [];
    }
}
