using Logic.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.X01;
using Shared.Interfaces.Training;
using Shared.Models.Shared;
using Shared.Models.Training;

namespace Logic.Integration.Tests.Training
{
    /// <summary>
    /// Review #30: Fehlercodes (400/409 statt 500), keine verwaisten offenen Trainings, Validierung beim Start,
    /// Around the Clock: Bull zählt für das Ziel „25“.
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class TrainingRulesTests : IAsyncLifetime
    {
        private readonly MySqlFixture _fixture;
        private TestHost _host = null!;

        public TrainingRulesTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            _host = await TestHost.CreateAsync(_fixture);
            await _host.SignInNewPlayerAsync();
        }

        public async Task DisposeAsync() => await _host.DisposeAsync();

        private Task<int> StartX01Async() => _host.RunAsync<IX01TrainingGame, int>(s => s.StartTrainingAsync(
            new StartX01TrainingModel { StartingScore = 501, InMode = X01InModeEnum.Straight, OutMode = X01OutModeEnum.Double }));

        [Fact]
        public async Task StartingAgain_FinishesTheOpenTrainingOfThatType()
        {
            var first = await StartX01Async();
            var second = await StartX01Async();

            var running = await _host.RunAsync<IX01TrainingGame, RunningX01Training?>(s => s.GetRunningTrainingAsync());
            var firstFinished = await _host.DbAsync(db => db.TrainingSessions.Where(s => s.Id == first).Select(s => s.FinishedAt).SingleAsync());

            Assert.Equal(second, running?.TrainingId);
            Assert.NotNull(firstFinished);
        }

        [Fact]
        public async Task ThrowIntoFinishedTraining_IsBusinessRule()
        {
            var id = await StartX01Async();
            await _host.RunAsync<IX01TrainingGame>(s => s.FinishTrainingAsync(id));

            await Assert.ThrowsAsync<BusinessRuleException>(() => _host.RunAsync<IX01TrainingGame, X01TrainingThrowResult>(s => s.AddThrowAsync(
                new X01TrainingThrow { TrainingId = id, VisitNumber = 1, ThrowNumber = 1, Segment = 20, Multiplier = 1 })));
        }

        [Theory]
        [InlineData(2, 1, 20, 1)]   // Aufnahme 2, bevor Aufnahme 1 existiert
        [InlineData(1, 2, 20, 1)]   // Dart 2 vor Dart 1
        [InlineData(1, 1, 25, 3)]   // Triple 25 gibt es nicht
        public async Task X01_InvalidThrow_IsArgumentException(int visit, int dart, int segment, int multiplier)
        {
            var id = await StartX01Async();

            await Assert.ThrowsAsync<ArgumentException>(() => _host.RunAsync<IX01TrainingGame, X01TrainingThrowResult>(s => s.AddThrowAsync(
                new X01TrainingThrow { TrainingId = id, VisitNumber = visit, ThrowNumber = dart, Segment = segment, Multiplier = multiplier })));
        }

        [Theory]
        [InlineData(159)]
        [InlineData(169)]
        public async Task Checkout_ImpossibleTarget_IsRejected(int target)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _host.RunAsync<ICheckoutTraining, int>(s => s.StartTrainingAsync(
                new StartCheckoutTrainingModel { TargetScore = target })));
        }

        [Theory]
        [InlineData(0, 0, 10)]     // Fehlwurf ist kein Ziel
        [InlineData(20, 3, 0)]     // keine Aufnahmen
        [InlineData(20, 3, 101)]   // zu viele Aufnahmen
        public async Task Segment_InvalidStart_IsRejected(int segment, int multiplier, int visits)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _host.RunAsync<ISegmentTraining, int>(s => s.StartTrainingAsync(
                new StartSegmentTrainingModel { TargetSegment = segment, TargetMultiplier = multiplier, NumberOfVisits = visits })));
        }

        [Fact]
        public async Task AroundTheClock_BullCountsForTarget25()
        {
            var id = await _host.RunAsync<IAroundTheClockTraining, int>(s => s.StartTrainingAsync(new StartAroundTheClockTrainingModel()));

            // 1–20 der Reihe nach treffen, danach Bull (50) auf das Ziel „25“.
            var darts = Enumerable.Range(1, 20).Select(n => (Segment: n, Multiplier: 1)).Append((25, 2)).ToList();
            AroundTheClockTrainingThrowResult? last = null;

            for (var i = 0; i < darts.Count; i++)
            {
                var (segment, multiplier) = darts[i];
                var visit = i / 3 + 1;
                var dart = i % 3 + 1;
                last = await _host.RunAsync<IAroundTheClockTraining, AroundTheClockTrainingThrowResult>(s => s.AddThrowAsync(
                    new AroundTheClockTrainingThrow { TrainingId = id, VisitNumber = visit, ThrowNumber = dart, Segment = segment, Multiplier = multiplier }));
            }

            Assert.NotNull(last);
            Assert.True(last.IsHit);
            Assert.Equal(21, last.PreviousTarget);
            Assert.Equal(22, last.CurrentTarget);
        }

        [Fact]
        public async Task AroundTheClock_ThrowAfterCompletion_IsBusinessRule()
        {
            var id = await _host.RunAsync<IAroundTheClockTraining, int>(s => s.StartTrainingAsync(new StartAroundTheClockTrainingModel()));
            await _host.RunAsync<IAroundTheClockTraining>(s => s.FinishTrainingAsync(id));

            await Assert.ThrowsAsync<BusinessRuleException>(() => _host.RunAsync<IAroundTheClockTraining, AroundTheClockTrainingThrowResult>(s => s.AddThrowAsync(
                new AroundTheClockTrainingThrow { TrainingId = id, VisitNumber = 1, ThrowNumber = 1, Segment = 1, Multiplier = 1 })));
        }
    }
}
