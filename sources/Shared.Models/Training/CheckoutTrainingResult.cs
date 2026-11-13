namespace Shared.Models.Training
{
    public class CheckoutTrainingResult
    {
        public int TrainingId { get; set; }
        public int TargetScore { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public bool IsFinished => FinishedAt.HasValue;
        public int TotalAttempts { get; set; }
        public int SuccessfulAttempts { get; set; }
        public int FailedAttempts { get; set; }
        public decimal CheckoutRate { get; set; }
        public int TotalThrows { get; set; }
        public int TotalScore { get; set; }
        public decimal AverageScorePerThrow { get; set; }
        public decimal AverageThrowsPerAttempt { get; set; }
        public int BestCheckout { get; set; }
        public int FewestDartsForCheckout { get; set; }
        public decimal AverageDartsPerCheckout { get; set; }
    }
}
