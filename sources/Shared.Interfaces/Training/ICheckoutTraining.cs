using Shared.Models.Training;

namespace Shared.Interfaces.Training
{
    public interface ICheckoutTraining
    {
        Task<int> StartTrainingAsync(StartCheckoutTrainingModel model);

        Task AddThrowAsync(CheckoutTrainingThrow model);

        Task FinishTrainingAsync(int trainingId);

        Task<CheckoutTrainingResult> GetResultAsync(int trainingId);
    }
}
