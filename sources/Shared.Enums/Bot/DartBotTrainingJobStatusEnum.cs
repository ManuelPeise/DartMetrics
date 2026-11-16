namespace Shared.Enums.Bot
{
    /// <summary>Zustand eines Trainingsauftrags aus dem Admin-Bereich (abgearbeitet vom BotTrainer-Worker).</summary>
    public enum DartBotTrainingJobStatusEnum
    {
        Queued = 1,
        Running = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5
    }
}
