namespace Shared.Models.Training
{
    public class AroundTheClockTrainingThrowResult
    {
        public bool IsHit { get; set; }

        public int PreviousTarget { get; set; }

        public int CurrentTarget { get; set; }

        public bool IsFinished { get; set; }
    }
}
