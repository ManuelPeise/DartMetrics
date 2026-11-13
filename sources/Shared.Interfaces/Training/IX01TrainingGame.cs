using Shared.Models.Training;

namespace Shared.Interfaces.Training
{
    public interface IX01TrainingGame
    {
        Task<int> StartTrainingAsync(StartX01TrainingModel model);
        Task<X01TrainingThrowResult> AddThrowAsync(X01TrainingThrow model);
        Task FinishTrainingAsync(int trainingId);
        Task<X01TrainingResult> GetResultAsync(int trainingId);
    }
}
