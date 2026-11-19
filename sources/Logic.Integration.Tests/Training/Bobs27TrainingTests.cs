using Logic.Integration.Tests.Infrastructure;
using Shared.Interfaces.Training;
using Shared.Models.Shared;
using Shared.Models.Training;

namespace Logic.Integration.Tests.Training
{
    /// <summary>Bob's 27 mit Datenbank: Start, Züge, Ausscheiden, Undo, Fortsetzen, Schutz vor doppeltem Senden.</summary>
    [Collection(MySqlCollection.Name)]
    public class Bobs27TrainingTests : IAsyncLifetime
    {
        private readonly MySqlFixture _fixture;
        private TestHost _host = null!;

        public Bobs27TrainingTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            _host = await TestHost.CreateAsync(_fixture);
            await _host.SignInNewPlayerAsync();
        }

        public async Task DisposeAsync() => await _host.DisposeAsync();

        private Task<Bobs27TrainingState> StartAsync(bool beginner = false, params string[] names) =>
            _host.RunAsync<IBobs27Training, Bobs27TrainingState>(s => s.StartTrainingAsync(
                new StartBobs27TrainingModel { PlayerNames = names.Length == 0 ? new List<string> { "Ich" } : names.ToList(), IsBeginnerMode = beginner }));

        private Task<Bobs27TrainingState> TurnAsync(Bobs27TrainingState state, int hits) =>
            _host.RunAsync<IBobs27Training, Bobs27TrainingState>(s => s.AddTurnAsync(
                new Bobs27TrainingTurnModel { TrainingId = state.TrainingId, TurnNumber = state.NextTurnNumber, Hits = hits }));

        [Fact]
        public async Task Start_ShowsD1With27Points()
        {
            var state = await StartAsync();

            Assert.Equal(1, state.Round);
            Assert.Equal("D1", state.TargetLabel);
            Assert.Equal(2, state.TargetValue);
            Assert.Equal(27, state.Players.Single().Score);
            Assert.Equal(1, state.CurrentPlayerNumber);
            Assert.False(state.CanUndo);
        }

        [Fact]
        public async Task Turns_ScoreAndBust_EndTheTraining()
        {
            var state = await StartAsync();

            state = await TurnAsync(state, 2);            // D1: 27 + 4 = 31
            Assert.Equal(31, state.Players[0].Score);
            Assert.Equal("D2", state.TargetLabel);

            foreach (var _ in Enumerable.Range(0, 4))
            {
                state = await TurnAsync(state, 0);        // −4 −6 −8 −10 = 3
            }

            Assert.Equal(3, state.Players[0].Score);
            state = await TurnAsync(state, 0);            // D6: 3 − 12 = −9 → raus

            Assert.True(state.IsFinished);
            Assert.True(state.Players[0].IsEliminated);
            Assert.Equal(6, state.Players[0].EliminatedInRound);

            var running = await _host.RunAsync<IBobs27Training, Bobs27TrainingState?>(s => s.GetRunningTrainingAsync());
            Assert.Null(running);
        }

        [Fact]
        public async Task UndoAfterBust_ReopensTheGame()
        {
            var state = await StartAsync();
            foreach (var _ in Enumerable.Range(0, 5))
            {
                state = await TurnAsync(state, 0);        // raus in Runde 5 (−3)
            }

            Assert.True(state.IsFinished);
            Assert.True(state.CanUndo);

            state = await _host.RunAsync<IBobs27Training, Bobs27TrainingState>(s => s.UndoLastTurnAsync(
                new Bobs27TrainingUndoModel { TrainingId = state.TrainingId, TurnNumber = state.NextTurnNumber - 1 }));

            Assert.False(state.IsFinished);
            Assert.Equal(5, state.Round);
            Assert.Equal(7, state.Players[0].Score);

            var running = await _host.RunAsync<IBobs27Training, Bobs27TrainingState?>(s => s.GetRunningTrainingAsync());
            Assert.Equal(state.TrainingId, running?.TrainingId);
        }

        [Fact]
        public async Task SameTurnTwice_IsRejected()
        {
            var state = await StartAsync();
            await TurnAsync(state, 1);

            // Gleiche TurnNumber noch einmal (z. B. Doppelklick, zweites Gerät).
            await Assert.ThrowsAsync<BusinessRuleException>(() => TurnAsync(state, 1));
        }

        [Fact]
        public async Task Multiplayer_TurnOrderAndResume()
        {
            var state = await StartAsync(false, "Anna", "Ben");

            state = await TurnAsync(state, 1);            // Anna D1
            Assert.Equal(2, state.CurrentPlayerNumber);
            state = await TurnAsync(state, 0);            // Ben D1
            Assert.Equal(1, state.CurrentPlayerNumber);
            Assert.Equal(2, state.Round);

            var running = await _host.RunAsync<IBobs27Training, Bobs27TrainingState?>(s => s.GetRunningTrainingAsync());
            Assert.NotNull(running);
            Assert.Equal(new[] { "Anna", "Ben" }, running.Players.Select(p => p.Name));
            Assert.Equal(new[] { 29, 25 }, running.Players.Select(p => p.Score));
            Assert.Equal(3, running.NextTurnNumber);
        }

        [Fact]
        public async Task BeginnerMode_ContinuesBelowZero()
        {
            var state = await StartAsync(true);
            foreach (var _ in Enumerable.Range(0, 6))
            {
                state = await TurnAsync(state, 0);        // 27 − 2 − 4 − 6 − 8 − 10 − 12 = −15
            }

            Assert.False(state.IsFinished);
            Assert.Equal(-15, state.Players[0].Score);
            Assert.Equal(7, state.Round);
        }

        [Theory]
        [InlineData("")]                    // keine Spieler
        [InlineData("  ")]                  // leerer Name
        [InlineData("A,B,C,D,E,F,G,H,I")]   // mehr als 8 Spieler
        public async Task Start_InvalidPlayers_IsRejected(string commaSeparatedNames)
        {
            var names = commaSeparatedNames.Length == 0 ? new List<string>() : commaSeparatedNames.Split(',').ToList();

            await Assert.ThrowsAsync<ArgumentException>(() => _host.RunAsync<IBobs27Training, Bobs27TrainingState>(s => s.StartTrainingAsync(
                new StartBobs27TrainingModel { PlayerNames = names })));
        }

        [Fact]
        public async Task InvalidHits_IsRejected()
        {
            var state = await StartAsync();

            await Assert.ThrowsAsync<ArgumentException>(() => TurnAsync(state, 4));
        }
    }
}
