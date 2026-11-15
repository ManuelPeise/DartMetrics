using Logic.Integration.Tests.Infrastructure;
using Shared.Enums.X01;
using Shared.Interfaces.Training;
using Shared.Models.Training;

namespace Logic.Integration.Tests.Training
{
    /// <summary>Offenes Training nach Reload: neuer Scope = neue Anfrage, Stand kommt nur aus der Datenbank.</summary>
    [Collection(MySqlCollection.Name)]
    public class TrainingResumeTests : IAsyncLifetime
    {
        private readonly MySqlFixture _fixture;
        private TestHost _host = null!;

        public TrainingResumeTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            _host = await TestHost.CreateAsync(_fixture);
            await _host.SignInNewPlayerAsync();
        }

        public async Task DisposeAsync() => await _host.DisposeAsync();

        [Fact]
        public async Task X01_MidVisit_ResumesAtThirdDart()
        {
            var id = await _host.RunAsync<IX01TrainingGame, int>(s => s.StartTrainingAsync(
                new StartX01TrainingModel { StartingScore = 501, InMode = X01InModeEnum.Straight, OutMode = X01OutModeEnum.Double }));

            await _host.RunAsync<IX01TrainingGame, X01TrainingThrowResult>(s => s.AddThrowAsync(
                new X01TrainingThrow { TrainingId = id, VisitNumber = 1, ThrowNumber = 1, Segment = 20, Multiplier = 3 }));
            await _host.RunAsync<IX01TrainingGame, X01TrainingThrowResult>(s => s.AddThrowAsync(
                new X01TrainingThrow { TrainingId = id, VisitNumber = 1, ThrowNumber = 2, Segment = 19, Multiplier = 1 }));

            var running = await _host.RunAsync<IX01TrainingGame, RunningX01Training?>(s => s.GetRunningTrainingAsync());

            Assert.NotNull(running);
            Assert.Equal(id, running.TrainingId);
            Assert.Equal(1, running.NextVisitNumber);
            Assert.Equal(3, running.NextThrowNumber);
            Assert.Equal(501 - 79, running.RemainingScore);
            Assert.Equal(2, running.CurrentVisitDarts.Count);
        }

        [Fact]
        public async Task X01_DoubleIn_NotOpened_StaysClosed()
        {
            var id = await _host.RunAsync<IX01TrainingGame, int>(s => s.StartTrainingAsync(
                new StartX01TrainingModel { StartingScore = 301, InMode = X01InModeEnum.Double, OutMode = X01OutModeEnum.Double }));

            await _host.RunAsync<IX01TrainingGame, X01TrainingThrowResult>(s => s.AddThrowAsync(
                new X01TrainingThrow { TrainingId = id, VisitNumber = 1, ThrowNumber = 1, Segment = 20, Multiplier = 3 }));

            var running = await _host.RunAsync<IX01TrainingGame, RunningX01Training?>(s => s.GetRunningTrainingAsync());

            Assert.NotNull(running);
            Assert.False(running.IsOpened);
            Assert.Equal(301, running.RemainingScore);
        }

        [Fact]
        public async Task AroundTheClock_HitThenMiss_ResumesOnTargetTwo()
        {
            var id = await _host.RunAsync<IAroundTheClockTraining, int>(s => s.StartTrainingAsync(new StartAroundTheClockTrainingModel()));

            await _host.RunAsync<IAroundTheClockTraining, AroundTheClockTrainingThrowResult>(s => s.AddThrowAsync(
                new AroundTheClockTrainingThrow { TrainingId = id, VisitNumber = 1, ThrowNumber = 1, Segment = 1, Multiplier = 1 }));
            await _host.RunAsync<IAroundTheClockTraining, AroundTheClockTrainingThrowResult>(s => s.AddThrowAsync(
                new AroundTheClockTrainingThrow { TrainingId = id, VisitNumber = 1, ThrowNumber = 2, Segment = 0, Multiplier = 0 }));

            var running = await _host.RunAsync<IAroundTheClockTraining, RunningAroundTheClockTraining?>(s => s.GetRunningTrainingAsync());

            Assert.NotNull(running);
            Assert.Equal(2, running.CurrentTarget);
            Assert.Equal(1, running.Hits);
            Assert.Equal(3, running.NextThrowNumber);
        }

        [Fact]
        public async Task Segment_AfterFourDarts_ResumesInSecondVisit()
        {
            var id = await _host.RunAsync<ISegmentTraining, int>(s => s.StartTrainingAsync(
                new StartSegmentTrainingModel { TargetSegment = 20, TargetMultiplier = 3, NumberOfVisits = 5 }));

            for (var dart = 0; dart < 4; dart++)
            {
                var visit = dart / 3 + 1;
                var throwNumber = dart % 3 + 1;
                await _host.RunAsync<ISegmentTraining, SegmentTrainingThrowResult>(s => s.AddThrowAsync(
                    new SegmentTrainingThrow { TrainingId = id, VisitNumber = visit, ThrowNumber = throwNumber, Segment = 20, Multiplier = 3 }));
            }

            var running = await _host.RunAsync<ISegmentTraining, RunningSegmentTraining?>(s => s.GetRunningTrainingAsync());

            Assert.NotNull(running);
            Assert.Equal(2, running.NextVisitNumber);
            Assert.Equal(2, running.NextThrowNumber);
            Assert.Equal(4, running.Hits);
            Assert.Single(running.CurrentVisitDarts);
        }

        [Fact]
        public async Task Checkout_OpenAttempt_ResumesWithRemainingScore()
        {
            var id = await _host.RunAsync<ICheckoutTraining, int>(s => s.StartTrainingAsync(new StartCheckoutTrainingModel { TargetScore = 40 }));

            await _host.RunAsync<ICheckoutTraining, CheckoutTrainingThrowResult>(s => s.AddThrowAsync(
                new CheckoutTrainingThrow { TrainingId = id, AttemptNumber = 1, ThrowNumber = 1, Segment = 20, Multiplier = 1 }));

            var running = await _host.RunAsync<ICheckoutTraining, RunningCheckoutTraining?>(s => s.GetRunningTrainingAsync());

            Assert.NotNull(running);
            Assert.Equal(1, running.NextVisitNumber);
            Assert.Equal(2, running.NextThrowNumber);
            Assert.Equal(20, running.RemainingScore);
            Assert.NotEmpty(running.CheckoutSuggestions);
        }

        [Fact]
        public async Task FinishedTraining_IsNotOfferedForResume()
        {
            var id = await _host.RunAsync<IX01TrainingGame, int>(s => s.StartTrainingAsync(
                new StartX01TrainingModel { StartingScore = 501 }));

            await _host.RunAsync<IX01TrainingGame>(s => s.FinishTrainingAsync(id));

            var running = await _host.RunAsync<IX01TrainingGame, RunningX01Training?>(s => s.GetRunningTrainingAsync());
            Assert.Null(running);
        }
    }
}
