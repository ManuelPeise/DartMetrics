
using Shared.Models.Shared;

namespace Logic.Shared.Rules
{
    public class X01Rules : DartRulesBase
    {
        public bool IsBust(int remainingScore, int visitScore, int lastSegment, int lastMultiplier)
        {
            var newRemainingScore = remainingScore - visitScore;

            return newRemainingScore < 0 ||
                   newRemainingScore == 1 ||
                   (newRemainingScore == 0 && !IsDouble(lastSegment, lastMultiplier));
        }

        public bool IsCheckout(int remainingScore, int visitScore, int lastSegment, int lastMultiplier)
        {
            return remainingScore - visitScore == 0 &&
                   IsDouble(lastSegment, lastMultiplier);
        }

        public List<CheckoutSuggestion> GetCheckoutSuggestions(int remainingScore, int remainingDarts)
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
                suggestions);

            return suggestions;
        }

        private void FindCheckoutWays(
            int remainingScore,
            int remainingDarts,
            List<CheckoutDart> possibleThrows,
            List<CheckoutDart> currentWay,
            List<CheckoutSuggestion> suggestions)
        {
            foreach (var dart in possibleThrows)
            {
                var newRemainingScore = remainingScore - dart.Score;
                var newWay = currentWay.Append(dart).ToList();

                if (newRemainingScore == 0 && IsDouble(dart.Segment, dart.Multiplier))
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
                    suggestions);
            }
        }
    }
}

