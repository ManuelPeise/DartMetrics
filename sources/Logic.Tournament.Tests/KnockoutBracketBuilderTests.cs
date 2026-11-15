using Logic.Tournament.Knockout;

namespace Logic.Tournament.Tests
{
    public class KnockoutBracketBuilderTests
    {
        [Fact]
        public void SeedPositions_KeepTopSeedsApart()
        {
            Assert.Equal([1, 4, 2, 3], KnockoutBracketBuilder.SeedPositions(4));
            Assert.Equal([1, 8, 4, 5, 2, 7, 3, 6], KnockoutBracketBuilder.SeedPositions(8));
        }

        [Theory]
        [InlineData(4, 4, 2)]
        [InlineData(5, 8, 3)]
        [InlineData(12, 16, 4)]
        [InlineData(16, 16, 4)]
        [InlineData(17, 32, 5)]
        public void Size_And_Rounds(int participants, int size, int rounds)
        {
            Assert.Equal(size, KnockoutBracketBuilder.BracketSize(participants));
            Assert.Equal(rounds, KnockoutBracketBuilder.RoundCount(participants));
        }

        [Fact]
        public void TwelveWithFourSeeds_TopFourGetByes()
        {
            var seeded = new[] { 1, 2, 3, 4 };
            var unseeded = Enumerable.Range(5, 8).ToList();

            var bracket = KnockoutBracketBuilder.Build(seeded, unseeded);
            var first = bracket.Where(m => m.Round == 1).ToList();

            Assert.Equal(8, first.Count);
            Assert.Equal(4, first.Count(m => m.IsBye));
            Assert.All(first.Where(m => m.IsBye), m => Assert.Contains(m.Home ?? m.Away!.Value, seeded));
            Assert.Equal(8 + 4 + 2 + 1, bracket.Count);
        }

        [Fact]
        public void SeedsOneAndTwo_MeetOnlyInFinal()
        {
            var bracket = KnockoutBracketBuilder.Build([1, 2, 3, 4], Enumerable.Range(5, 12).ToList());
            var first = bracket.Where(m => m.Round == 1).ToList();

            var half1 = first.Take(4).SelectMany(m => new[] { m.Home, m.Away }).ToList();
            var half2 = first.Skip(4).SelectMany(m => new[] { m.Home, m.Away }).ToList();

            Assert.Contains(1, half1);
            Assert.Contains(2, half2);

            // Platz 3 und 4 in verschiedenen Vierteln, keiner mit 1 oder 2 im selben Viertel.
            var quarters = Enumerable.Range(0, 4).Select(q => first.Skip(q * 2).Take(2).SelectMany(m => new[] { m.Home, m.Away }).ToList()).ToList();
            Assert.All(new[] { 1, 2, 3, 4 }, seed => Assert.Single(quarters, q => q.Contains(seed)));
            Assert.All(quarters, q => Assert.Single(q, id => id is >= 1 and <= 4));
        }

        [Fact]
        public void Winners_FeedNextRound()
        {
            var bracket = KnockoutBracketBuilder.Build([], Enumerable.Range(1, 8).ToList());

            var final = Assert.Single(bracket, m => m.NextMatchNumber is null);
            Assert.Equal(3, final.Round);

            foreach (var round in new[] { 1, 2 })
            {
                var targets = bracket.Where(m => m.Round == round).Select(m => (m.NextMatchNumber, m.NextSlot)).ToList();
                Assert.Equal(targets.Count, targets.Distinct().Count());
            }

            Assert.All(bracket.Where(m => m.Round > 1), m => Assert.True(m.Home is null && m.Away is null));
        }

        [Fact]
        public void RoundNames_CountFromFinal()
        {
            Assert.Equal("Finale", KnockoutBracketBuilder.RoundName(4, 4));
            Assert.Equal("Halbfinale", KnockoutBracketBuilder.RoundName(3, 4));
            Assert.Equal("Viertelfinale", KnockoutBracketBuilder.RoundName(2, 4));
            Assert.Equal("Achtelfinale", KnockoutBracketBuilder.RoundName(1, 4));
            Assert.Equal("Runde 1", KnockoutBracketBuilder.RoundName(1, 5));
        }

        [Theory]
        [InlineData(3)]
        [InlineData(65)]
        public void WrongSize_Throws(int n) =>
            Assert.Throws<ArgumentException>(() => KnockoutBracketBuilder.Build([], Enumerable.Range(1, n).ToList()));
    }
}
