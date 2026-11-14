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

        public bool IsOpened { get; set; }

        public int Busts { get; set; }

        public List<CheckoutSuggestion> CheckoutSuggestions { get; set; } = [];
    }
}
