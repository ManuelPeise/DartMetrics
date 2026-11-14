namespace Shared.Models.Training
{
    public class RunningSegmentTraining : RunningTrainingBase
    {
        public int TargetSegment { get; set; }

        public int TargetMultiplier { get; set; }

        public int NumberOfVisits { get; set; }

        public int Hits { get; set; }
    }
}
