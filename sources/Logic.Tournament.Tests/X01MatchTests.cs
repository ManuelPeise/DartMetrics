using Logic.Tournament.Match;
using Shared.Enums.X01;

namespace Logic.Tournament.Tests
{
    public class X01MatchTests
    {
        private static X01Match NewMatch(X01InModeEnum inMode = X01InModeEnum.Straight, X01OutModeEnum outMode = X01OutModeEnum.Double, int bestOf = 5) =>
            new(501, inMode, outMode, bestOf);

        private static void Visit(X01Match match, params (int Segment, int Multiplier)[] darts)
        {
            foreach (var (segment, multiplier) in darts)
            {
                match.Throw(segment, multiplier);
            }
        }

        /// <summary>Ein 9-Darter für den Spieler am Wurf, dazwischen 3 Misses des Gegners.</summary>
        private static void NineDarter(X01Match match)
        {
            var player = match.CurrentPlayer;
            var legsBefore = match.LegsWon(player);
            Visit(match, (20, 3), (20, 3), (20, 3));
            Visit(match, (0, 0), (0, 0), (0, 0));
            Visit(match, (20, 3), (20, 3), (20, 3));
            Visit(match, (0, 0), (0, 0), (0, 0));
            Visit(match, (20, 3), (19, 3), (12, 2));
            Assert.Equal(legsBefore + 1, match.LegsWon(player));
        }

        [Fact]
        public void Visit_SwitchesPlayerAfterThreeDarts()
        {
            var match = NewMatch();

            Visit(match, (20, 3), (20, 1), (5, 1));

            Assert.Equal(501 - 85, match.Remaining(1));
            Assert.Equal(2, match.CurrentPlayer);
            Assert.Empty(match.CurrentVisit);
        }

        [Fact]
        public void Bust_ResetsToVisitStartAndEndsVisit()
        {
            var match = NewMatch();
            NineDarterUntil(match, 40);

            var outcome = match.Throw(20, 3);

            Assert.True(outcome.IsBust);
            Assert.True(outcome.VisitEnded);
            Assert.Equal(40, match.Remaining(1));
            Assert.Equal(2, match.CurrentPlayer);
        }

        [Fact]
        public void DoubleOut_RestOneIsBust_StraightOutFinishesWithSingle()
        {
            var doubleOut = NewMatch();
            NineDarterUntil(doubleOut, 40);
            Assert.True(doubleOut.Throw(13, 3).IsBust);

            var straightOut = NewMatch(outMode: X01OutModeEnum.Straight);
            NineDarterUntil(straightOut, 40);
            straightOut.Throw(20, 1);
            var outcome = straightOut.Throw(20, 1);
            Assert.True(outcome.IsCheckout);
            Assert.Equal(1, outcome.LegWinner);
        }

        [Fact]
        public void Checkout_StartsNextLegWithOtherStarter()
        {
            var match = NewMatch();
            NineDarter(match);

            Assert.Equal(1, match.LegsWon(1));
            Assert.Equal(2, match.LegNumber);
            Assert.Equal(2, match.LegStarter);
            Assert.Equal(2, match.CurrentPlayer);
            Assert.Equal(501, match.Remaining(1));
            Assert.Equal(501, match.Remaining(2));
        }

        [Fact]
        public void Match_EndsAtMajority()
        {
            var match = NewMatch(bestOf: 5);

            // Wer anwirft, gewinnt mit einem 9-Darter: 1:0, 1:1, 2:1, 2:2, 3:2 für Spieler 1.
            NineDarter(match);
            NineDarter(match);
            NineDarter(match);
            Assert.Equal(2, match.LegsWon(1));
            Assert.Equal(1, match.LegsWon(2));

            NineDarter(match);
            Assert.False(match.IsFinished);
            NineDarter(match);

            Assert.True(match.IsFinished);
            Assert.Equal(1, match.Winner);
            Assert.Equal(3, match.LegsWon(1));
            Assert.Throws<InvalidOperationException>(() => match.Throw(20, 1));
        }

        [Fact]
        public void DoubleIn_CountsFromOpeningDart()
        {
            var match = NewMatch(inMode: X01InModeEnum.Double);

            Visit(match, (20, 3), (20, 2), (20, 1));
            Assert.Equal(501 - 60, match.Remaining(1));
            Assert.True(match.IsOpened(1));

            Visit(match, (20, 3), (20, 3), (20, 3));
            Assert.Equal(501, match.Remaining(2));
            Assert.False(match.IsOpened(2));
        }

        [Fact]
        public void Replay_RestoresSameState()
        {
            var darts = new List<(int, int)> { (20, 3), (20, 3), (20, 3), (19, 1), (0, 0), (5, 1), (20, 1) };
            var played = NewMatch();
            foreach (var (s, m) in darts)
            {
                played.Throw(s, m);
            }

            var replayed = X01Match.Replay(501, X01InModeEnum.Straight, X01OutModeEnum.Double, 5, darts);

            Assert.Equal(played.Remaining(1), replayed.Remaining(1));
            Assert.Equal(played.Remaining(2), replayed.Remaining(2));
            Assert.Equal(played.CurrentPlayer, replayed.CurrentPlayer);
            Assert.Equal(played.CurrentVisit.Count, replayed.CurrentVisit.Count);
        }

        [Theory]
        [InlineData(25, 3)]
        [InlineData(21, 1)]
        [InlineData(0, 1)]
        public void InvalidDart_Throws(int segment, int multiplier) =>
            Assert.Throws<ArgumentException>(() => NewMatch().Throw(segment, multiplier));

        [Fact]
        public void InvalidBestOf_Throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new X01Match(501, X01InModeEnum.Straight, X01OutModeEnum.Double, 3));

        /// <summary>Spieler 1 auf genau <paramref name="rest"/> bringen (Spieler 2 wirft Misses), Spieler 1 ist danach am Wurf.</summary>
        private static void NineDarterUntil(X01Match match, int rest)
        {
            // 501 - 180 - 180 = 141; dann 141 - rest in einer Aufnahme.
            Visit(match, (20, 3), (20, 3), (20, 3));
            Visit(match, (0, 0), (0, 0), (0, 0));
            Visit(match, (20, 3), (20, 3), (20, 3));
            Visit(match, (0, 0), (0, 0), (0, 0));

            Assert.Equal(40, rest); // T20 + D17 + S7 = 101 → Rest 40
            Visit(match, (20, 3), (17, 2), (7, 1));
            Visit(match, (0, 0), (0, 0), (0, 0));
            Assert.Equal(rest, match.Remaining(1));
            Assert.Equal(1, match.CurrentPlayer);
        }
    }
}
