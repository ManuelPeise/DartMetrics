using Shared.Models.Training;

namespace Shared.Interfaces.Training
{
    public interface IAroundTheClockTraining
    {
        /// <summary>Neuestes offenes Training dieser Art des aktuellen Benutzers, sonst null.</summary>
        Task<RunningAroundTheClockTraining?> GetRunningTrainingAsync();

        Task<int> StartTrainingAsync(StartAroundTheClockTrainingModel model);

        Task<AroundTheClockTrainingThrowResult> AddThrowAsync(AroundTheClockTrainingThrow model);

        Task FinishTrainingAsync(int trainingId);

        Task<AroundTheClockTrainingResult> GetResultAsync(int trainingId);
    }
}
