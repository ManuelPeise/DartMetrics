using Logic.Shared.Rules;

namespace Logic.Bot.Tests.Rules
{
    /// <summary>Bob's 27: Zielwerte, Punkteberechnung, Bust bei 0 oder weniger, Höchstwert 1437, Spielablauf, Sieger.</summary>
    public class Bobs27Tests
    {
        [Theory]
        [InlineData(1, 2, "D1")]
        [InlineData(2, 4, "D2")]
        [InlineData(16, 32, "D16")]
        [InlineData(20, 40, "D20")]
        [InlineData(21, 50, "Bull")]
        public void Targets_AreD1ToD20ThenBull(int round, int value, string label)
        {
            Assert.Equal(value, Bobs27Rules.TargetValue(round));
            Assert.Equal(label, Bobs27Rules.TargetLabel(round));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(22)]
        public void TargetValue_OutsideTheRounds_Throws(int round) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => Bobs27Rules.TargetValue(round));

        [Theory]
        [InlineData(27, 1, 0, 25)]    // D1 verfehlt: −2
        [InlineData(27, 1, 1, 29)]    // 1 × D1: +2
        [InlineData(27, 1, 3, 33)]    // 3 × D1: +6
        [InlineData(27, 16, 2, 91)]   // 2 × D16: +64
        [InlineData(27, 16, 0, -5)]   // D16 verfehlt: −32
        [InlineData(100, 21, 3, 250)] // 3 × Bull: +150
        [InlineData(100, 21, 0, 50)]  // Bull verfehlt: −50
        public void ApplyRound_AddsHitsOrSubtractsTarget(int score, int round, int hits, int expected) =>
            Assert.Equal(expected, Bobs27Rules.ApplyRound(score, round, hits));

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        public void ApplyRound_InvalidHits_Throws(int hits) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => Bobs27Rules.ApplyRound(27, 1, hits));

        [Theory]
        [InlineData(1, false)]
        [InlineData(0, true)]    // 0 ist schon Game Over
        [InlineData(-3, true)]
        public void IsBust_AtZeroOrBelow(int score, bool expected)
        {
            Assert.Equal(expected, Bobs27Rules.IsBust(score, beginnerMode: false));
            Assert.False(Bobs27Rules.IsBust(score, beginnerMode: true));
        }

        [Fact]
        public void PerfectGame_Scores1437()
        {
            var game = Bobs27Game.Replay(1, false, Enumerable.Repeat(3, Bobs27Rules.RoundCount));

            Assert.True(game.IsFinished);
            Assert.Equal(Bobs27Rules.MaxScore, game.Players[0].Score);
            Assert.Equal(1437, game.Players[0].Score);
            Assert.Equal(21, game.Round);
        }

        [Fact]
        public void Singleplayer_BustEndsTheGameImmediately()
        {
            // 27 − 2 − 4 − 6 − 8 = 7, D5 verfehlt: 7 − 10 = −3 → raus in Runde 5, keine weiteren Runden.
            var game = Bobs27Game.Replay(1, false, [0, 0, 0, 0, 0]);

            Assert.True(game.IsFinished);
            Assert.Null(game.CurrentPlayer);
            Assert.Equal(5, game.Players[0].EliminatedInRound);
            Assert.Equal(-3, game.Players[0].Score);
            Assert.True(game.Turns[^1].IsBust);
        }

        [Fact]
        public void ExactlyZero_IsBust()
        {
            // Hinweis: Ab 27 mit lauter geraden Zielwerten bleibt der Stand immer ungerade – genau 0 kommt im echten
            // Spiel nicht vor. Die Regel gilt trotzdem: 0 ist schon Game Over.
            Assert.Equal(0, Bobs27Rules.ApplyRound(2, 1, 0));
            Assert.True(Bobs27Rules.IsBust(0, beginnerMode: false));
            Assert.False(Bobs27Rules.IsBust(1, beginnerMode: false));
        }

        [Fact]
        public void BeginnerMode_PlaysAllRoundsBelowZero()
        {
            var game = Bobs27Game.Replay(1, true, Enumerable.Repeat(0, Bobs27Rules.RoundCount));

            Assert.True(game.IsFinished);
            Assert.False(game.Players[0].IsEliminated);
            Assert.Equal(21, game.Turns.Count);
            // 27 − (2 + 4 + … + 40) − 50 = 27 − 420 − 50
            Assert.Equal(27 - 420 - 50, game.Players[0].Score);
        }

        [Fact]
        public void Multiplayer_EliminatedPlayerIsSkipped_OthersPlayOn()
        {
            var game = new Bobs27Game(2, false);

            // Runde 1–5: Spieler 1 trifft je einmal, Spieler 2 verfehlt (27 − 2 − 4 − 6 − 8 − 10 = −3 in Runde 5).
            for (var round = 1; round <= 5; round++)
            {
                Assert.Equal(1, game.CurrentPlayer);
                game.Play(1);
                Assert.Equal(2, game.CurrentPlayer);
                game.Play(0);
            }

            Assert.True(game.Players[1].IsEliminated);
            Assert.Equal(5, game.Players[1].EliminatedInRound);

            // Ab Runde 6 nur noch Spieler 1.
            Assert.Equal(6, game.Round);
            Assert.Equal(1, game.CurrentPlayer);
            game.Play(1);
            Assert.Equal(7, game.Round);
            Assert.Equal(1, game.CurrentPlayer);
        }

        [Fact]
        public void Multiplayer_WinnerHasMostPointsAfterRound21()
        {
            var hits = new List<int>();
            for (var round = 1; round <= Bobs27Rules.RoundCount; round++)
            {
                hits.Add(2);   // Spieler 1
                hits.Add(1);   // Spieler 2
            }

            var game = Bobs27Game.Replay(2, false, hits);

            Assert.True(game.IsFinished);
            Assert.Equal(new[] { 1 }, game.Winners);
        }

        [Fact]
        public void Multiplayer_AllEliminated_FurthestWins()
        {
            // Spieler 1 verfehlt alles (raus in Runde 5), Spieler 2 trifft Runde 1 einmal (29, dann −4 −6 −8 −10 = 1,
            // −12 → raus in Runde 6).
            var game = Bobs27Game.Replay(2, false, [0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0]);

            Assert.True(game.IsFinished);
            Assert.Equal(5, game.Players[0].EliminatedInRound);
            Assert.Equal(6, game.Players[1].EliminatedInRound);
            Assert.Equal(new[] { 2 }, game.Winners);
        }

        [Fact]
        public void Multiplayer_Tie_HasTwoWinners()
        {
            var game = Bobs27Game.Replay(2, false, Enumerable.Repeat(3, 2 * Bobs27Rules.RoundCount));

            Assert.Equal(new[] { 1, 2 }, game.Winners);
        }

        [Fact]
        public void PlayAfterTheEnd_Throws()
        {
            var game = Bobs27Game.Replay(1, false, Enumerable.Repeat(3, Bobs27Rules.RoundCount));

            Assert.Throws<InvalidOperationException>(() => game.Play(1));
        }
    }
}
