using Shared.Models.Training;

namespace Shared.Interfaces.Training
{
    public interface IAroundTheClockTraining
    {
        Task<int> StartTrainingAsync(StartAroundTheClockTrainingModel model);

        Task<AroundTheClockTrainingThrowResult> AddThrowAsync(AroundTheClockTrainingThrow model);

        Task FinishTrainingAsync(int trainingId);

        Task<AroundTheClockTrainingResult> GetResultAsync(int trainingId);
    }
}
