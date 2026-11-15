using Shared.Enums.X01;
using Shared.Models.Shared;

namespace Logic.Bot.Tests.Rules
{
    /// <summary>Checkout-Empfehlung für Menschen (Review-Finding T5): alle Reste 2–170, unmögliche Werte, bevorzugte Wege.</summary>
    public class CheckoutAdvisorTests
    {
        private static readonly int[] ImpossibleDoubleOut = [159, 162, 163, 165, 166, 168, 169];

        [Theory]
        [InlineData(170, 3, "T20 T20 D25")]
        [InlineData(100, 3, "T20 D20")]
        [InlineData(40, 3, "D20")]
        [InlineData(32, 1, "D16")]
        [InlineData(50, 1, "D25")]
        [InlineData(60, 2, "S20 D20")]
        [InlineData(98, 3, "T20 D19")]
        [InlineData(160, 3, "T20 T20 D20")]
        public void BestRoute_IsTheStandardWay(int remaining, int darts, string expected)
        {
            var best = CheckoutAdvisor.GetBestRoutes(remaining, darts)[0];

            Assert.Equal(expected, string.Join(" ", best.Darts.Select(Label)));
        }

        [Fact]
        public void EveryCheckableRest_HasAValidRoute_WithFewestDarts()
        {
            for (var remaining = 2; remaining <= 170; remaining++)
            {
                var routes = CheckoutAdvisor.GetBestRoutes(remaining, 3);

                if (ImpossibleDoubleOut.Contains(remaining))
                {
                    Assert.Empty(routes);
                    continue;
                }

                Assert.NotEmpty(routes);
                Assert.True(routes.Count <= CheckoutAdvisor.MaxRoutes);

                foreach (var route in routes)
                {
                    Assert.InRange(route.Darts.Count, 1, 3);
                    Assert.Equal(remaining, route.Darts.Sum(d => d.Score));
                    Assert.Equal(2, route.Darts[^1].Multiplier);

                    // Kein Weg darf vor dem letzten Dart auf 1 oder 0 stehen.
                    var rest = remaining;
                    foreach (var dart in route.Darts.Take(route.Darts.Count - 1))
                    {
                        rest -= dart.Score;
                        Assert.True(rest >= 2, $"{remaining}: Zwischenrest {rest}");
                    }
                }

                // Alle Wege nutzen die kleinstmögliche Dartanzahl.
                Assert.Single(routes.Select(r => r.Darts.Count).Distinct());
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(171)]
        [InlineData(180)]
        public void OutOfRange_HasNoRoute(int remaining)
        {
            Assert.Empty(CheckoutAdvisor.GetBestRoutes(remaining, 3));
        }

        [Fact]
        public void FewerDartsLeft_NarrowsTheRoutes()
        {
            Assert.NotEmpty(CheckoutAdvisor.GetBestRoutes(100, 2));
            Assert.Empty(CheckoutAdvisor.GetBestRoutes(100, 1));
            Assert.Empty(CheckoutAdvisor.GetBestRoutes(41, 1));
            Assert.Empty(CheckoutAdvisor.GetBestRoutes(40, 0));
        }

        [Fact]
        public void StraightOut_AllowsAnyFinish()
        {
            Assert.Equal("T20 T20 T20", string.Join(" ", CheckoutAdvisor.GetBestRoutes(180, 3, X01OutModeEnum.Straight)[0].Darts.Select(Label)));
            Assert.NotEmpty(CheckoutAdvisor.GetBestRoutes(1, 1, X01OutModeEnum.Straight));
        }

        [Fact]
        public void ReturnedRoutes_AreCopies()
        {
            var first = CheckoutAdvisor.GetBestRoutes(40, 3);
            first[0].Darts.Clear();

            Assert.NotEmpty(CheckoutAdvisor.GetBestRoutes(40, 3)[0].Darts);
        }

        private static string Label(CheckoutDart dart) => dart.Multiplier switch
        {
            3 => $"T{dart.Segment}",
            2 => $"D{dart.Segment}",
            _ => $"S{dart.Segment}"
        };
    }
}
