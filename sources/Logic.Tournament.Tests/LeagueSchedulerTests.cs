using Logic.Tournament.League;

namespace Logic.Tournament.Tests
{
    public class LeagueSchedulerTests
    {
        public static TheoryData<int> Sizes => new() { 8, 9, 12, 15, 16 };

        private static List<int> Ids(int n) => Enumerable.Range(101, n).ToList();

        [Theory]
        [MemberData(nameof(Sizes))]
        public void DoubleRoundRobin_HasExpectedMatchdays(int n)
        {
            var days = LeagueScheduler.CreateDoubleRoundRobin(Ids(n));
            var slots = n % 2 == 0 ? n : n + 1;

            Assert.Equal(2 * (slots - 1), days.Count);
        }

        [Theory]
        [MemberData(nameof(Sizes))]
        public void EveryPairMeetsTwice_WithSwappedHome(int n)
        {
            var matches = LeagueScheduler.CreateDoubleRoundRobin(Ids(n))
                .SelectMany(d => d)
                .Where(p => p.Away is not null)
                .Select(p => (p.Home, Away: p.Away!.Value))
                .ToList();

            Assert.Equal(n * (n - 1), matches.Count);
            Assert.Equal(matches.Count, matches.Distinct().Count());

            foreach (var (home, away) in matches)
            {
                Assert.Contains((away, home), matches);
            }
        }

        [Theory]
        [MemberData(nameof(Sizes))]
        public void EachParticipantPlaysAtMostOncePerDay_ByeOnlyWhenOdd(int n)
        {
            foreach (var day in LeagueScheduler.CreateDoubleRoundRobin(Ids(n)))
            {
                var involved = day.SelectMany(p => p.Away is null ? new[] { p.Home } : new[] { p.Home, p.Away.Value }).ToList();

                Assert.Equal(involved.Count, involved.Distinct().Count());
                Assert.Equal(n, involved.Count);
                Assert.Equal(n % 2, day.Count(p => p.Away is null));
            }
        }

        [Theory]
        [MemberData(nameof(Sizes))]
        public void HomeGames_AreBalanced(int n)
        {
            var homeCounts = LeagueScheduler.CreateDoubleRoundRobin(Ids(n))
                .SelectMany(d => d)
                .Where(p => p.Away is not null)
                .GroupBy(p => p.Home)
                .Select(g => g.Count())
                .ToList();

            // Hin- und Rückrunde gleichen sich aus: jeder hat genau n−1 Heimspiele.
            Assert.All(homeCounts, c => Assert.Equal(n - 1, c));
        }

        [Theory]
        [InlineData(7)]
        [InlineData(17)]
        public void WrongSize_Throws(int n) =>
            Assert.Throws<ArgumentException>(() => LeagueScheduler.CreateDoubleRoundRobin(Ids(n)));

        [Fact]
        public void DuplicateParticipants_Throw() =>
            Assert.Throws<ArgumentException>(() => LeagueScheduler.CreateDoubleRoundRobin([1, 2, 3, 4, 5, 6, 7, 1]));
    }
}
