using Data.DartContext.Entities.Bot;
using Logic.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Bot;
using Shared.Enums.User;
using Shared.Interfaces.Administration;
using Shared.Models.Bot;
using Shared.Models.Shared;

namespace Logic.Integration.Tests.Bot
{
    /// <summary>Trainingsaufträge aus dem Admin-Bereich: anlegen, nur einer gleichzeitig, abbrechen, verlorene Aufträge.</summary>
    [Collection(MySqlCollection.Name)]
    public class TrainingJobAdministrationTests : IAsyncLifetime
    {
        private readonly MySqlFixture _fixture;
        private TestHost _host = null!;

        public TrainingJobAdministrationTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            _host = await TestHost.CreateAsync(_fixture);

            // Aufträge anderer Tests würden "nur einer gleichzeitig" verfälschen.
            await _host.DbAsync(db => db.Set<DartBotTrainingJobEntity>().ExecuteDeleteAsync());

            var adminId = await _host.CreateUserAsync("Admin", "User");
            _host.Claims.SignIn(adminId, UserRoleEnum.Admin);
        }

        public async Task DisposeAsync() => await _host.DisposeAsync();

        [Fact]
        public async Task CreateJob_UsesPresetParameters_AndIsQueued()
        {
            var jobId = await CreateAsync(DartBotTrainingJobPresetEnum.Standard);

            var job = Assert.Single(await GetJobsAsync());
            var (situations, _, legs) = DartBotTrainingPresets.For(DartBotTrainingJobPresetEnum.Standard);

            Assert.Equal(jobId, job.Id);
            Assert.Equal(DartBotTrainingJobStatusEnum.Queued, job.Status);
            Assert.Equal(situations, job.Situations);
            Assert.Equal(legs, job.CompareLegs);
            Assert.False(string.IsNullOrWhiteSpace(job.RequestedBy));
        }

        [Fact]
        public async Task SecondJob_WhileOneIsQueued_IsRejected()
        {
            await CreateAsync(DartBotTrainingJobPresetEnum.Quick);

            await Assert.ThrowsAsync<BusinessRuleException>(() => CreateAsync(DartBotTrainingJobPresetEnum.Quick));
        }

        [Fact]
        public async Task CancelQueuedJob_IsCancelledImmediately_AndAllowsNewJob()
        {
            var jobId = await CreateAsync(DartBotTrainingJobPresetEnum.Quick);

            await _host.RunAsync<IDartBotTrainingJobAdministration>(a => a.CancelJobAsync(jobId));

            var job = Assert.Single(await GetJobsAsync());
            Assert.Equal(DartBotTrainingJobStatusEnum.Cancelled, job.Status);
            Assert.NotNull(job.FinishedAt);

            await CreateAsync(DartBotTrainingJobPresetEnum.Quick);
            Assert.Equal(2, (await GetJobsAsync()).Count);
        }

        [Fact]
        public async Task CancelRunningJob_OnlyRequestsCancel()
        {
            var jobId = await CreateAsync(DartBotTrainingJobPresetEnum.Quick);
            await SetRunningAsync(jobId, heartbeatAt: DateTime.UtcNow);

            await _host.RunAsync<IDartBotTrainingJobAdministration>(a => a.CancelJobAsync(jobId));

            var job = Assert.Single(await GetJobsAsync());
            Assert.Equal(DartBotTrainingJobStatusEnum.Running, job.Status);
            Assert.True(job.CancelRequested);
        }

        [Fact]
        public async Task RunningJobWithoutHeartbeat_IsMarkedFailed()
        {
            var jobId = await CreateAsync(DartBotTrainingJobPresetEnum.Quick);
            await SetRunningAsync(jobId, heartbeatAt: DateTime.UtcNow.AddHours(-1));

            var job = Assert.Single(await GetJobsAsync());
            Assert.Equal(DartBotTrainingJobStatusEnum.Failed, job.Status);
            Assert.NotNull(job.ErrorMessage);
        }

        [Fact]
        public async Task CreateJob_WithUnknownBaseModel_IsRejected()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _host.RunAsync<IDartBotTrainingJobAdministration, int>(a => a.CreateJobAsync(new CreateDartBotTrainingJobModel
                {
                    Preset = DartBotTrainingJobPresetEnum.Quick,
                    BaseModelId = 999_999
                })));

            Assert.Empty(await GetJobsAsync());
        }

        [Fact]
        public async Task Player_CannotCreateJobs()
        {
            await _host.SignInNewPlayerAsync();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => CreateAsync(DartBotTrainingJobPresetEnum.Quick));
        }

        private Task<int> CreateAsync(DartBotTrainingJobPresetEnum preset) =>
            _host.RunAsync<IDartBotTrainingJobAdministration, int>(a => a.CreateJobAsync(new CreateDartBotTrainingJobModel { Preset = preset }));

        private Task<List<DartBotTrainingJobListItem>> GetJobsAsync() =>
            _host.RunAsync<IDartBotTrainingJobAdministration, List<DartBotTrainingJobListItem>>(a => a.GetJobsAsync());

        private Task SetRunningAsync(int jobId, DateTime heartbeatAt) =>
            _host.DbAsync(db => db.Set<DartBotTrainingJobEntity>()
                .Where(j => j.Id == jobId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(j => j.Status, DartBotTrainingJobStatusEnum.Running)
                    .SetProperty(j => j.StartedAt, DateTime.UtcNow)
                    .SetProperty(j => j.HeartbeatAt, heartbeatAt)));
    }
}
