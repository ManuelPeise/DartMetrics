using Shared.Enums.X01;
using Shared.Models.Shared;

namespace Shared.Models.Training
{
    public class RunningX01Training : RunningTrainingBase
    {
        public int StartingScore { get; set; }

        public X01InModeEnum InMode { get; set; }

        public X01OutModeEnum OutMode { get; set; }

        public int RemainingScore { get; set; }

        /// <summary>Aktuelles Leg (ab 1) und bereits gecheckte Legs im Satz.</summary>
        public int LegNumber { get; set; } = 1;

        public int LegsCompleted { get; set; }

        public int NumberOfLegs { get; set; } = 1;

        public bool IsOpened { get; set; }

        public int Busts { get; set; }

        public List<CheckoutSuggestion> CheckoutSuggestions { get; set; } = [];
    }
}
