using global::Shared.Models.Training;

namespace Web.App.Client.Services.Api
{
    /// <summary>
    /// Trainings des Spielers. Die TrainingId ist die Id der Trainingssession (Rückgabe von Start…).
    /// Jeder Dart liefert ein Ergebnis; die Oberfläche bildet keine Dart-Regeln nach.
    /// </summary>
    public interface ITrainingApi
    {
        // X01
        /// <summary>Offene Trainings zum Fortsetzen; Value ist null, wenn nichts offen ist.</summary>
        Task<ApiResult<RunningX01Training?>> GetRunningX01Async(CancellationToken cancellationToken = default);
        Task<ApiResult<RunningAroundTheClockTraining?>> GetRunningAroundTheClockAsync(CancellationToken cancellationToken = default);
        Task<ApiResult<RunningCheckoutTraining?>> GetRunningCheckoutAsync(CancellationToken cancellationToken = default);
        Task<ApiResult<RunningSegmentTraining?>> GetRunningSegmentAsync(CancellationToken cancellationToken = default);

        Task<ApiResult<int>> StartX01Async(StartX01TrainingModel model, CancellationToken cancellationToken = default);
        Task<ApiResult<X01TrainingThrowResult>> AddX01ThrowAsync(X01TrainingThrow model, CancellationToken cancellationToken = default);
        Task<ApiResult<bool>> FinishX01Async(int trainingId, CancellationToken cancellationToken = default);
        Task<ApiResult<X01TrainingResult>> GetX01ResultAsync(int trainingId, CancellationToken cancellationToken = default);

        // Around the Clock
        Task<ApiResult<int>> StartAroundTheClockAsync(CancellationToken cancellationToken = default);
        Task<ApiResult<AroundTheClockTrainingThrowResult>> AddAroundTheClockThrowAsync(AroundTheClockTrainingThrow model, CancellationToken cancellationToken = default);
        Task<ApiResult<bool>> FinishAroundTheClockAsync(int trainingId, CancellationToken cancellationToken = default);
        Task<ApiResult<AroundTheClockTrainingResult>> GetAroundTheClockResultAsync(int trainingId, CancellationToken cancellationToken = default);

        // Bob's 27 (jede Aktion liefert den vollständigen Stand)
        Task<ApiResult<Bobs27TrainingState?>> GetRunningBobs27Async(CancellationToken cancellationToken = default);
        Task<ApiResult<Bobs27TrainingState>> StartBobs27Async(StartBobs27TrainingModel model, CancellationToken cancellationToken = default);
        Task<ApiResult<Bobs27TrainingState>> AddBobs27TurnAsync(Bobs27TrainingTurnModel model, CancellationToken cancellationToken = default);
        Task<ApiResult<Bobs27TrainingState>> UndoBobs27TurnAsync(Bobs27TrainingUndoModel model, CancellationToken cancellationToken = default);
        Task<ApiResult<bool>> FinishBobs27Async(int trainingId, CancellationToken cancellationToken = default);
        Task<ApiResult<Bobs27TrainingState>> GetBobs27Async(int trainingId, CancellationToken cancellationToken = default);

        // Checkout
        Task<ApiResult<int>> StartCheckoutAsync(StartCheckoutTrainingModel model, CancellationToken cancellationToken = default);
        Task<ApiResult<CheckoutTrainingThrowResult>> AddCheckoutThrowAsync(CheckoutTrainingThrow model, CancellationToken cancellationToken = default);
        Task<ApiResult<bool>> FinishCheckoutAsync(int trainingId, CancellationToken cancellationToken = default);
        Task<ApiResult<CheckoutTrainingResult>> GetCheckoutResultAsync(int trainingId, CancellationToken cancellationToken = default);

        // Segment
        Task<ApiResult<int>> StartSegmentAsync(StartSegmentTrainingModel model, CancellationToken cancellationToken = default);
        Task<ApiResult<SegmentTrainingThrowResult>> AddSegmentThrowAsync(SegmentTrainingThrow model, CancellationToken cancellationToken = default);
        Task<ApiResult<bool>> FinishSegmentAsync(int trainingId, CancellationToken cancellationToken = default);
        Task<ApiResult<SegmentTrainingResult>> GetSegmentResultAsync(int trainingId, CancellationToken cancellationToken = default);
    }
}
