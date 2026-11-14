using Shared.Enums.X01;

namespace Shared.Models.Training
{
    public class X01TrainingResult
    {
        public int TrainingId { get; set; }
        public int StartingScore { get; set; }

        public X01InModeEnum InMode { get; set; }

        public X01OutModeEnum OutMode { get; set; }
        public int RemainingScore { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public bool IsFinished => FinishedAt.HasValue;
        public bool IsCheckout => RemainingScore == 0;
        public int TotalVisits { get; set; }
        public int TotalThrows { get; set; }
        public int TotalScore { get; set; }
        public int Busts { get; set; }
        public int SuccessfulVisits { get; set; }
        public decimal AverageScorePerThrow { get; set; }
        public decimal AverageScorePerVisit { get; set; }
        public int SingleHits { get; set; }
        public int DoubleHits { get; set; }
        public int TripleHits { get; set; }
        public int Misses { get; set; }
    }
}
