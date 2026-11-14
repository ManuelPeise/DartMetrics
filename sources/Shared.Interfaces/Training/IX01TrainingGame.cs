using Shared.Models.Training;

namespace Shared.Interfaces.Training
{
    public interface IX01TrainingGame
    {
        /// <summary>Neuestes offenes Training dieser Art des aktuellen Benutzers, sonst null.</summary>
        Task<RunningX01Training?> GetRunningTrainingAsync();

        Task<int> StartTrainingAsync(StartX01TrainingModel model);
        Task<X01TrainingThrowResult> AddThrowAsync(X01TrainingThrow model);
        Task FinishTrainingAsync(int trainingId);
        Task<X01TrainingResult> GetResultAsync(int trainingId);
    }
}
