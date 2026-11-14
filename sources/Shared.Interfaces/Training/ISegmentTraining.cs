using Shared.Models.Training;

namespace Shared.Interfaces.Training
{
    public interface ISegmentTraining
    {
        Task<int> StartTrainingAsync(StartSegmentTrainingModel model);

        Task<SegmentTrainingThrowResult> AddThrowAsync(SegmentTrainingThrow model);

        Task FinishTrainingAsync(int trainingId);

        Task<SegmentTrainingResult> GetResultAsync(int trainingId);
    }
}
