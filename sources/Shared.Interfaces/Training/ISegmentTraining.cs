using Shared.Models.Training;

namespace Shared.Interfaces.Training
{
    public interface ISegmentTraining
    {
        /// <summary>Neuestes offenes Training dieser Art des aktuellen Benutzers, sonst null.</summary>
        Task<RunningSegmentTraining?> GetRunningTrainingAsync();

        Task<int> StartTrainingAsync(StartSegmentTrainingModel model);

        Task<SegmentTrainingThrowResult> AddThrowAsync(SegmentTrainingThrow model);

        Task FinishTrainingAsync(int trainingId);

        Task<SegmentTrainingResult> GetResultAsync(int trainingId);
    }
}
