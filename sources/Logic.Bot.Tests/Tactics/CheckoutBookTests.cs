using Logic.Bot.Tactics;
using Logic.Shared.Rules;
using Shared.Enums.X01;
using Shared.Models.Bot;

namespace Logic.Bot.Tests.Tactics
{
    public class CheckoutBookTests
    {
        private static readonly int[] Bogeys = [169, 168, 166, 165, 163, 162, 159];
        private readonly X01Rules _rules = new();

        [Fact]
        public void EveryRoute_AddsUp_AndEndsOnDouble()
        {
            for (var remaining = 2; remaining <= 170; remaining++)
            {
                foreach (var darts in new[] { 1, 2, 3 })
                {
                    var route = CheckoutBook.Route(remaining, darts);

                    if (route is null || route.Length > darts)
                    {
                        continue;
                    }

                    // Ein Setup-Weg (1 Dart, ungerader Rest) checkt nicht; alle anderen müssen genau aufgehen.
                    if (route.Sum(t => t.Score) != remaining)
                    {
                        Assert.True(darts == 1 && route.Length == 1 && !route[0].IsDouble, $"{remaining} mit {darts}: {string.Join(" ", route)}");
                        continue;
                    }

                    var last = route[^1];
                    Assert.True(_rules.IsFinishingDart(last.Segment, last.Multiplier, X01OutModeEnum.Double), $"{remaining}: {string.Join(" ", route)}");
                }
            }
        }

        [Fact]
        public void AllBookRoutes_AreValid()
        {
            for (var remaining = 41; remaining <= 170; remaining++)
            {
                foreach (var route in CheckoutBook.RoutesFor(remaining))
                {
                    Assert.Equal(remaining, route.Sum(t => t.Score));
                    Assert.InRange(route.Length, 1, 3);
                    Assert.True(route[^1].IsDouble, $"{remaining}: {string.Join(" ", route)}");
                }
            }
        }

        [Fact]
        public void EveryCheckout_41To170_HasRoute_ExceptBogeys()
        {
            for (var remaining = 41; remaining <= 170; remaining++)
            {
                Assert.Equal(!Bogeys.Contains(remaining), CheckoutBook.Covers(remaining, 3));
            }
        }

        [Theory]
        [InlineData(170, 3, "T20")]
        [InlineData(121, 3, "T20")]
        [InlineData(101, 2, "T17")]  // 2 Darts: T17 Bull
        [InlineData(107, 2, "T19")]  // 2 Darts: T19 Bull
        [InlineData(100, 2, "T20")]
        [InlineData(82, 2, "Bull")]
        [InlineData(61, 3, "T15")]
        [InlineData(40, 1, "D20")]
        [InlineData(50, 1, "Bull")]
        [InlineData(33, 3, "S1")]    // S1 D16
        [InlineData(31, 2, "S15")]   // S15 D8
        [InlineData(7, 1, "S3")]     // Setup auf D2
        [InlineData(2, 1, "D1")]
        public void NextTarget_FollowsTheBook(int remaining, int darts, string expected)
        {
            Assert.Equal(expected, CheckoutBook.NextTarget(remaining, darts)?.ToString());
        }

        [Fact]
        public void AfterMiss_RouteIsRecalculated()
        {
            // 121: T20 geplant, S20 getroffen → 101 mit 2 Darts → T17, Bull.
            Assert.Equal("T20", CheckoutBook.NextTarget(121, 3)?.ToString());
            Assert.Equal(["T17", "Bull"], CheckoutBook.Route(101, 2)!.Select(t => t.ToString()));
        }

        [Fact]
        public void NoRoute_ForBogeyOrTooHigh()
        {
            Assert.Null(CheckoutBook.NextTarget(169, 3));
            Assert.Null(CheckoutBook.NextTarget(171, 3));
            Assert.Null(CheckoutBook.NextTarget(120, 1));
        }
    }
}
