using Logic.Statistics.Performance;
using Shared.Enums.Statistics;
using Shared.Enums.X01;

namespace Logic.Statistics.Tests
{
    /// <summary>
    /// Baut Legs wie die echte Wertung: Reststand je Dart, Bust (unter 0, Rest 1 oder 0 ohne Double bei Double-Out), Checkout.
    /// Darts als Kurzschrift: "T20", "D16", "S5", "25" (Single-Bull), "BULL" (50), "0" (Miss).
    /// </summary>
    internal static class LegBuilder
    {
        private static int _unit;

        public static StatLeg Leg(int start, params string[][] visits) =>
            Leg(start, X01OutModeEnum.Double, isWon: true, visits);

        public static StatLeg Leg(int start, X01OutModeEnum outMode, bool isWon, params string[][] visits) =>
            Leg($"u{++_unit}", DateTime.UtcNow, StatisticsCategoryEnum.Training, start, outMode, isWon, visits);

        public static StatLeg Leg(string unit, DateTime date, StatisticsCategoryEnum category, int start, X01OutModeEnum outMode, bool isWon,
            params string[][] visits)
        {
            var remaining = start;
            var result = new List<StatVisit>();

            foreach (var visit in visits)
            {
                var before = remaining;
                var darts = new List<StatDart>();
                var bust = false;
                var checkout = false;

                foreach (var code in visit)
                {
                    var (segment, multiplier) = Parse(code);
                    darts.Add(new StatDart(segment, multiplier, remaining));
                    var rest = remaining - segment * multiplier;

                    if (rest < 0 || (outMode == X01OutModeEnum.Double && (rest == 1 || (rest == 0 && multiplier != 2))))
                    {
                        bust = true;
                        break;
                    }

                    remaining = rest;

                    if (rest == 0)
                    {
                        checkout = true;
                        break;
                    }
                }

                if (bust)
                {
                    remaining = before;
                }

                result.Add(new StatVisit(bust ? 0 : before - remaining, bust, checkout, before, darts));

                if (checkout)
                {
                    break;
                }
            }

            var finished = result.Any(v => v.IsCheckout);
            return new StatLeg(unit, date, category, outMode, finished, finished && isWon, result);
        }

        public static string[] V(params string[] darts) => darts;

        private static (int Segment, int Multiplier) Parse(string code) => code switch
        {
            "0" => (0, 0),
            "25" => (25, 1),
            "BULL" => (25, 2),
            _ => (int.Parse(code[1..]), code[0] switch { 'T' => 3, 'D' => 2, _ => 1 })
        };
    }
}
