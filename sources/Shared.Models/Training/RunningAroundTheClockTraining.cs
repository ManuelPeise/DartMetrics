namespace Shared.Models.Training
{
    public class RunningAroundTheClockTraining : RunningTrainingBase
    {
        /// <summary>1–20, 21 = 25, 22 = Bull.</summary>
        public int CurrentTarget { get; set; } = 1;

        public int Hits { get; set; }
    }
}
