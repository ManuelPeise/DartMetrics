using Shared.Models.Shared;

namespace Shared.Models.Training
{
    public class X01TrainingThrowResult
    {
        public int RemainingScore { get; set; }
        public int RemainingDarts { get; set; }
        public bool IsBust { get; set; }
        public bool IsCheckout { get; set; }

        /// <summary>Bei Double-In: ist das Leg nach diesem Dart eröffnet?</summary>
        public bool IsOpened { get; set; } = true;

        public List<CheckoutSuggestion> CheckoutSuggestions { get; set; } = [];
    }
}
