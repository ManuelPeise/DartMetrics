using global::Shared.Models.Training;

namespace Web.App.Client.Services.Api
{
    /// <summary>Routen: api/&lt;Controller&gt;/&lt;Methodenname&gt; (Service.Api, [action]).</summary>
    public sealed class TrainingApi : ITrainingApi
    {
        private const string X01 = "X01TrainingGame/";
        private const string AroundTheClock = "AroundTheClockTraining/";
        private const string Checkout = "CheckoutTraining/";
        private const string Segment = "SegmentTraining/";

        private readonly ApiClient _api;

        public TrainingApi(ApiClient api)
        {
            _api = api;
        }

        // Offene Trainings (204 = nichts offen)
        public Task<ApiResult<RunningX01Training?>> GetRunningX01Async(CancellationToken ct = default) =>
            ApiResponse.ReadOptionalAsync<RunningX01Training>(_api.GetAsync(X01 + "GetRunningX01Training", ct), ct);

        public Task<ApiResult<RunningAroundTheClockTraining?>> GetRunningAroundTheClockAsync(CancellationToken ct = default) =>
            ApiResponse.ReadOptionalAsync<RunningAroundTheClockTraining>(_api.GetAsync(AroundTheClock + "GetRunningAroundTheClockTraining", ct), ct);

        public Task<ApiResult<RunningCheckoutTraining?>> GetRunningCheckoutAsync(CancellationToken ct = default) =>
            ApiResponse.ReadOptionalAsync<RunningCheckoutTraining>(_api.GetAsync(Checkout + "GetRunningCheckoutTraining", ct), ct);

        public Task<ApiResult<RunningSegmentTraining?>> GetRunningSegmentAsync(CancellationToken ct = default) =>
            ApiResponse.ReadOptionalAsync<RunningSegmentTraining>(_api.GetAsync(Segment + "GetRunningSegmentTraining", ct), ct);

        // X01
        public Task<ApiResult<int>> StartX01Async(StartX01TrainingModel model, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<int>(_api.PostAsJsonAsync(X01 + "StartX01Training", model, ct), ct);

        public Task<ApiResult<X01TrainingThrowResult>> AddX01ThrowAsync(X01TrainingThrow model, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<X01TrainingThrowResult>(_api.PostAsJsonAsync(X01 + "AddX01TrainingThrow", model, ct), ct);

        public Task<ApiResult<bool>> FinishX01Async(int trainingId, CancellationToken ct = default) =>
            ApiResponse.ExecuteAsync(_api.PostAsJsonAsync(X01 + "FinishX01Training", new TrainingSessionRequestModel { TrainingId = trainingId }, ct), ct);

        public Task<ApiResult<X01TrainingResult>> GetX01ResultAsync(int trainingId, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<X01TrainingResult>(_api.GetAsync($"{X01}GetX01TrainingResult?trainingId={trainingId}", ct), ct);

        // Around the Clock
        public Task<ApiResult<int>> StartAroundTheClockAsync(CancellationToken ct = default) =>
            ApiResponse.ReadAsync<int>(_api.PostAsJsonAsync(AroundTheClock + "StartTraining", new StartAroundTheClockTrainingModel(), ct), ct);

        public Task<ApiResult<AroundTheClockTrainingThrowResult>> AddAroundTheClockThrowAsync(AroundTheClockTrainingThrow model, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<AroundTheClockTrainingThrowResult>(_api.PostAsJsonAsync(AroundTheClock + "AddThrow", model, ct), ct);

        public Task<ApiResult<bool>> FinishAroundTheClockAsync(int trainingId, CancellationToken ct = default) =>
            ApiResponse.ExecuteAsync(_api.PostAsJsonAsync<object?>($"{AroundTheClock}FinishTraining?trainingId={trainingId}", null, ct), ct);

        public Task<ApiResult<AroundTheClockTrainingResult>> GetAroundTheClockResultAsync(int trainingId, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<AroundTheClockTrainingResult>(_api.GetAsync($"{AroundTheClock}GetResult?trainingId={trainingId}", ct), ct);

        // Checkout
        public Task<ApiResult<int>> StartCheckoutAsync(StartCheckoutTrainingModel model, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<int>(_api.PostAsJsonAsync(Checkout + "StartCheckoutTraining", model, ct), ct);

        public Task<ApiResult<CheckoutTrainingThrowResult>> AddCheckoutThrowAsync(CheckoutTrainingThrow model, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<CheckoutTrainingThrowResult>(_api.PostAsJsonAsync(Checkout + "AddCheckoutTrainingThrow", model, ct), ct);

        public Task<ApiResult<bool>> FinishCheckoutAsync(int trainingId, CancellationToken ct = default) =>
            ApiResponse.ExecuteAsync(_api.PostAsJsonAsync(Checkout + "FinishCheckoutTraining", new TrainingSessionRequestModel { TrainingId = trainingId }, ct), ct);

        public Task<ApiResult<CheckoutTrainingResult>> GetCheckoutResultAsync(int trainingId, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<CheckoutTrainingResult>(_api.GetAsync($"{Checkout}GetCheckoutTrainingResult?trainingId={trainingId}", ct), ct);

        // Segment
        public Task<ApiResult<int>> StartSegmentAsync(StartSegmentTrainingModel model, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<int>(_api.PostAsJsonAsync(Segment + "StartSegmentTraining", model, ct), ct);

        public Task<ApiResult<SegmentTrainingThrowResult>> AddSegmentThrowAsync(SegmentTrainingThrow model, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<SegmentTrainingThrowResult>(_api.PostAsJsonAsync(Segment + "AddSegmentTrainingThrow", model, ct), ct);

        public Task<ApiResult<bool>> FinishSegmentAsync(int trainingId, CancellationToken ct = default) =>
            ApiResponse.ExecuteAsync(_api.PostAsJsonAsync(Segment + "FinishSegmentTraining", new TrainingSessionRequestModel { TrainingId = trainingId }, ct), ct);

        public Task<ApiResult<SegmentTrainingResult>> GetSegmentResultAsync(int trainingId, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<SegmentTrainingResult>(_api.GetAsync($"{Segment}GetSegmentTrainingResult?trainingId={trainingId}", ct), ct);
    }
}
