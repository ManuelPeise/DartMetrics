using Logic.Tournament.League;

namespace Logic.Tournament.Tests
{
    public class LeagueTableCalculatorTests
    {
        private static readonly Dictionary<int, string> Players = new() { [1] = "Anna", [2] = "Bert", [3] = "Cleo", [4] = "Dirk" };

        private static LeagueMatchResult R(int home, int away, int homeLegs, int awayLegs, int homeAvg = 60, int awayAvg = 60) =>
            new(home, away, homeLegs, awayLegs, homeAvg * 10, 30, awayAvg * 10, 30);

        [Fact]
        public void Win_GivesTwoPoints_AndCountsLegs()
        {
            var table = LeagueTableCalculator.Calculate(Players, [R(1, 2, 3, 1)]);

            var anna = table.Single(e => e.ParticipantId == 1);
            var bert = table.Single(e => e.ParticipantId == 2);
            Assert.Equal(1, anna.Position);
            Assert.Equal(2, anna.Points);
            Assert.Equal(2, anna.LegDifference);
            Assert.Equal(0, bert.Points);
            Assert.Equal(1, bert.Won + bert.Lost);
            Assert.Equal(60, anna.Average);
        }

        [Fact]
        public void EqualPoints_DecidedByLegDifference_ThenLegsWon()
        {
            var table = LeagueTableCalculator.Calculate(Players,
            [
                R(1, 3, 3, 0), // Anna +3
                R(2, 4, 3, 2), // Bert +1
                R(3, 2, 3, 2), // Cleo +1, 3 Legs
                R(4, 1, 3, 0)  // Dirk +3, 5 Legs
            ]);

            // Alle 2 Punkte. Diff: Dirk +2 (5:3), Bert 0 (5:5), Anna 0 (3:3), Cleo −2 (3:5).
            Assert.Equal([4, 2, 1, 3], table.Select(e => e.ParticipantId));
        }

        [Fact]
        public void FullTie_DecidedByHeadToHead()
        {
            var two = new Dictionary<int, string> { [1] = "Anna", [2] = "Bert" };

            // Hin- und Rückspiel je 3:2: komplett gleich, direkter Vergleich auch – dann Average.
            var table = LeagueTableCalculator.Calculate(two, [R(1, 2, 3, 2, homeAvg: 50, awayAvg: 70), R(2, 1, 3, 2, homeAvg: 70, awayAvg: 50)]);

            Assert.Equal(2, table.First().ParticipantId);
        }

        [Fact]
        public void HeadToHead_BeatsAverage()
        {
            // Anna und Bert: je 2 Punkte, 4:4 Legs; Anna gewann das direkte Duell, Bert hat den besseren Average.
            var table = LeagueTableCalculator.Calculate(Players,
            [
                R(1, 2, 3, 1, homeAvg: 40, awayAvg: 90),
                R(2, 3, 3, 1, homeAvg: 90, awayAvg: 40),
                R(4, 1, 3, 1, homeAvg: 40, awayAvg: 40),
                R(4, 3, 3, 0)
            ]);

            Assert.Equal([4, 1, 2, 3], table.Select(e => e.ParticipantId));
        }

        [Fact]
        public void NoMatches_AllZero_SortedByName()
        {
            var table = LeagueTableCalculator.Calculate(Players, []);

            Assert.Equal(["Anna", "Bert", "Cleo", "Dirk"], table.Select(e => e.DisplayName));
            Assert.All(table, e => Assert.Equal(0, e.Points));
        }

        [Fact]
        public void Draw_IsRejected() =>
            Assert.Throws<ArgumentException>(() => LeagueTableCalculator.Calculate(Players, [R(1, 2, 2, 2)]));
    }
}
