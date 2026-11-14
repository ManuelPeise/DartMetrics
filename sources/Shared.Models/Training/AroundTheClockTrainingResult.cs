namespace Shared.Models.Training
{
    public class AroundTheClockTrainingResult
    {
        public int TotalThrows { get; set; }

        public int TotalHits { get; set; }

        public int TotalMisses { get; set; }

        public int SingleHits { get; set; }

        public int DoubleHits { get; set; }

        public int TripleHits { get; set; }

        public int OuterBullHits { get; set; }

        public int InnerBullHits { get; set; }

        public int TotalVisits { get; set; }

        public double HitRate { get; set; }

        public bool IsFinished { get; set; }

        public DateTime? FinishedAt { get; set; }
    }
}
