using Shared.Enums.X01;

namespace Shared.Models.Training
{
    public class StartX01TrainingModel
    {
        public int StartingScore { get; set; }

        public X01InModeEnum InMode { get; set; } = X01InModeEnum.Straight;

        public X01OutModeEnum OutMode { get; set; } = X01OutModeEnum.Double;
    }
}
