using Shared.Models.Training;

namespace Shared.Interfaces.Training
{
    public interface ICheckoutTraining
    {
        /// <summary>Neuestes offenes Training dieser Art des aktuellen Benutzers, sonst null.</summary>
        Task<RunningCheckoutTraining?> GetRunningTrainingAsync();

        Task<int> StartTrainingAsync(StartCheckoutTrainingModel model);

        Task<CheckoutTrainingThrowResult> AddThrowAsync(CheckoutTrainingThrow model);

        Task FinishTrainingAsync(int trainingId);

        Task<CheckoutTrainingResult> GetResultAsync(int trainingId);
    }
}
