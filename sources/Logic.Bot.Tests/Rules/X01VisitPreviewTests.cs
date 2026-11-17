using Logic.Shared.Rules;
using Shared.Enums.X01;
using Shared.Models.Shared;

namespace Logic.Bot.Tests.Rules
{
    /// <summary>
    /// Die Browser-Vorschau (X01VisitPreview) muss Bust und Checkout genau wie die Server-Regeln (X01Rules) erkennen.
    /// Vergleich über alle Darts × zwei Darts, Reste 1–80 und alle In-/Out-Modi.
    /// </summary>
    public class X01VisitPreviewTests
    {
        private static readonly X01Rules Rules = new();

        private static readonly (int Segment, int Multiplier)[] AllDarts = BuildAllDarts();

        [Theory]
        [InlineData(X01InModeEnum.Straight, X01OutModeEnum.Double)]
        [InlineData(X01InModeEnum.Straight, X01OutModeEnum.Straight)]
        [InlineData(X01InModeEnum.Double, X01OutModeEnum.Double)]
        [InlineData(X01InModeEnum.Double, X01OutModeEnum.Straight)]
        public void Preview_MatchesServerRules(X01InModeEnum inMode, X01OutModeEnum outMode)
        {
            foreach (var isOpened in new[] { true, false })
            {
                // Rest 1 ohne Eröffnung gibt es nicht (Startwert 301/501).
                for (var remaining = isOpened || inMode == X01InModeEnum.Straight ? 1 : 2; remaining <= 80; remaining++)
                {
                    foreach (var first in AllDarts)
                    {
                        foreach (var second in AllDarts)
                        {
                            var darts = new[] { first, second };
                            var expected = Reference(remaining, isOpened, inMode, outMode, darts);
                            var actual = X01VisitPreview.Evaluate(remaining, isOpened, inMode, outMode, 0, darts);

                            Assert.True(expected == (actual.IsBust, actual.IsCheckout, actual.Remaining),
                                $"Rest {remaining}, eröffnet {isOpened}, {inMode}/{outMode}, {first} {second}: erwartet {expected}, Vorschau {actual}");
                        }
                    }
                }
            }
        }

        [Fact]
        public void ThreeDarts_FinishTheVisit()
        {
            var result = X01VisitPreview.Evaluate(501, true, X01InModeEnum.Straight, X01OutModeEnum.Double, 1, [(20, 3), (20, 3)]);

            Assert.True(result.IsVisitFinished);
            Assert.Equal(381, result.Remaining);
        }

        [Fact]
        public void DoubleIn_CountsFromTheOpeningDart()
        {
            var result = X01VisitPreview.Evaluate(501, false, X01InModeEnum.Double, X01OutModeEnum.Double, 0, [(20, 3), (20, 2)]);

            Assert.True(result.IsOpened);
            Assert.Equal(461, result.Remaining);
            Assert.False(result.IsVisitFinished);
        }

        /// <summary>Wertung wie auf dem Server: nach jedem Dart IsBust/IsCheckout mit der gezählten Summe seit Aufnahmebeginn.</summary>
        private static (bool IsBust, bool IsCheckout, int Remaining) Reference(
            int remaining, bool isOpened, X01InModeEnum inMode, X01OutModeEnum outMode, (int Segment, int Multiplier)[] darts)
        {
            for (var i = 0; i < darts.Length; i++)
            {
                var counted = Rules.CountedScore(darts.Take(i + 1), isOpened, inMode);
                var (segment, multiplier) = darts[i];

                if (Rules.IsCheckout(remaining, counted, segment, multiplier, outMode))
                {
                    return (false, true, 0);
                }

                if (Rules.IsBust(remaining, counted, segment, multiplier, outMode))
                {
                    return (true, false, remaining);
                }
            }

            return (false, false, remaining - Rules.CountedScore(darts, isOpened, inMode));
        }

        private static (int, int)[] BuildAllDarts()
        {
            var darts = new List<(int, int)> { (0, 0), (25, 1), (25, 2) };

            for (var segment = 1; segment <= 20; segment++)
            {
                darts.Add((segment, 1));
                darts.Add((segment, 2));
                darts.Add((segment, 3));
            }

            return darts.ToArray();
        }
    }
}
