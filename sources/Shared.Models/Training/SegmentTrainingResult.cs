namespace Shared.Models.Training
{
    public class SegmentTrainingResult
    {
        public int TrainingId { get; set; }
        public int TargetSegment { get; set; }
        public int TargetMultiplier { get; set; }
        public int NumberOfVisits { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public bool IsFinished => FinishedAt.HasValue;
        public int TotalThrows { get; set; }
        public int TotalHits { get; set; }
        public int TotalScore { get; set; }
        public decimal HitRate { get; set; }
        public decimal AverageScorePerThrow { get; set; }
        public decimal AverageScorePerVisit { get; set; }
        public int TotalVisits { get; set; }
        public int SingleHits { get; set; }
        public int DoubleHits { get; set; }
        public int TripleHits { get; set; }
        public int TargetHits { get; set; }
        public int Misses { get; set; }
        public int BestVisitScore { get; set; }
        public decimal AverageHitsPerVisit { get; set; }
    }
}
