
using Shared.Enums.X01;
using Shared.Models.Shared;

namespace Logic.Shared.Rules
{
    /// <summary>
    /// X01-Regeln (301/501). Standard ist Straight-In / Double-Out; die Überladungen mit In-/Out-Modus
    /// unterstützen zusätzlich Double-In und Master-Out. Rest 1 ist in beiden Out-Modi ein Bust.
    /// </summary>
    public class X01Rules : DartRulesBase
    {
        public bool IsBust(int remainingScore, int visitScore, int lastSegment, int lastMultiplier) =>
            IsBust(remainingScore, visitScore, lastSegment, lastMultiplier, X01OutModeEnum.Double);

        public bool IsBust(int remainingScore, int visitScore, int lastSegment, int lastMultiplier, X01OutModeEnum outMode)
        {
            var newRemainingScore = remainingScore - visitScore;

            return newRemainingScore < 0 ||
                   newRemainingScore == 1 ||
                   (newRemainingScore == 0 && !IsFinishingDart(lastSegment, lastMultiplier, outMode));
        }

        public bool IsCheckout(int remainingScore, int visitScore, int lastSegment, int lastMultiplier) =>
            IsCheckout(remainingScore, visitScore, lastSegment, lastMultiplier, X01OutModeEnum.Double);

        public bool IsCheckout(int remainingScore, int visitScore, int lastSegment, int lastMultiplier, X01OutModeEnum outMode) =>
            remainingScore - visitScore == 0 && IsFinishingDart(lastSegment, lastMultiplier, outMode);

        /// <summary>Darf ein Leg mit diesem Dart beendet werden?</summary>
        public bool IsFinishingDart(int segment, int multiplier, X01OutModeEnum outMode) =>
            outMode switch
            {
                X01OutModeEnum.Master => IsDouble(segment, multiplier) || IsTriple(segment, multiplier),
                _ => IsDouble(segment, multiplier)
            };

        /// <summary>Eröffnet dieser Dart das Leg? Bei Straight-In immer, bei Double-In nur ein Double/Bull.</summary>
        public bool IsOpeningDart(int segment, int multiplier, X01InModeEnum inMode) =>
            inMode == X01InModeEnum.Straight || IsDouble(segment, multiplier);

        /// <summary>
        /// Punkte einer Folge von Darts unter Berücksichtigung von Double-In:
        /// vor der Eröffnung zählen Darts 0, der eröffnende Dart zählt bereits.
        /// </summary>
        public int CountedScore(IEnumerable<(int Segment, int Multiplier)> darts, bool isOpened, X01InModeEnum inMode)
        {
            var total = 0;
            var opened = isOpened || inMode == X01InModeEnum.Straight;

            foreach (var (segment, multiplier) in darts)
            {
                if (!opened && IsOpeningDart(segment, multiplier, inMode))
                {
                    opened = true;
                }

                if (opened)
                {
                    total += CalculateScore(segment, multiplier);
                }
            }

            return total;
        }

        public List<CheckoutSuggestion> GetCheckoutSuggestions(int remainingScore, int remainingDarts) =>
            GetCheckoutSuggestions(remainingScore, remainingDarts, X01OutModeEnum.Double);

        public List<CheckoutSuggestion> GetCheckoutSuggestions(int remainingScore, int remainingDarts, X01OutModeEnum outMode)
        {
            var suggestions = new List<CheckoutSuggestion>();

            if (remainingScore <= 1 || remainingDarts <= 0)
                return suggestions;

            var possibleThrows = new List<CheckoutDart>();

            for (var segment = 1; segment <= 20; segment++)
            {
                possibleThrows.Add(new CheckoutDart { Segment = segment, Multiplier = 1, Score = segment });
                possibleThrows.Add(new CheckoutDart { Segment = segment, Multiplier = 2, Score = segment * 2 });
                possibleThrows.Add(new CheckoutDart { Segment = segment, Multiplier = 3, Score = segment * 3 });
            }

            possibleThrows.Add(new CheckoutDart { Segment = 25, Multiplier = 1, Score = 25 });
            possibleThrows.Add(new CheckoutDart { Segment = 25, Multiplier = 2, Score = 50 });

            FindCheckoutWays(
                remainingScore,
                remainingDarts,
                possibleThrows,
                [],
                suggestions,
                outMode);

            return suggestions;
        }

        private void FindCheckoutWays(
            int remainingScore,
            int remainingDarts,
            List<CheckoutDart> possibleThrows,
            List<CheckoutDart> currentWay,
            List<CheckoutSuggestion> suggestions,
            X01OutModeEnum outMode)
        {
            foreach (var dart in possibleThrows)
            {
                var newRemainingScore = remainingScore - dart.Score;
                var newWay = currentWay.Append(dart).ToList();

                if (newRemainingScore == 0 && IsFinishingDart(dart.Segment, dart.Multiplier, outMode))
                {
                    suggestions.Add(new CheckoutSuggestion
                    {
                        Darts = newWay
                    });

                    continue;
                }

                if (newRemainingScore <= 1 || remainingDarts <= 1)
                    continue;

                FindCheckoutWays(
                    newRemainingScore,
                    remainingDarts - 1,
                    possibleThrows,
                    newWay,
                    suggestions,
                    outMode);
            }
        }
    }
}

