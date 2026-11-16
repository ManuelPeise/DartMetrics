using Shared.Models.Statistics;

namespace Logic.Statistics.Performance
{
    /// <summary>
    /// Feste, regelbasierte Hinweise aus dem Vergleich mit dem Vorzeitraum. Nur bei ausreichend Daten
    /// (je Zeitraum mindestens <see cref="MinLegs"/> abgeschlossene Legs) und deutlichen Änderungen.
    /// </summary>
    public static class PerformanceInsights
    {
        public const int MinLegs = 10;

        /// <summary>Ab dieser Änderung (Punkte) gilt ein Average als verbessert bzw. verschlechtert.</summary>
        public const decimal AverageThreshold = 2m;

        /// <summary>Ab dieser Änderung (Prozentpunkte) gilt eine Quote als verbessert bzw. verschlechtert.</summary>
        public const decimal RateThreshold = 3m;

        public static List<string> Build(PerformanceKpis kpis, int legs, int previousLegs)
        {
            var insights = new List<string>();

            if (legs < MinLegs || previousLegs < MinLegs)
            {
                return insights;
            }

            var scoring = Trend(kpis.ThreeDartAverage.Change, AverageThreshold);
            var start = Trend(kpis.FirstNineAverage.Change, AverageThreshold);
            var finishing = Trend(kpis.CheckoutRate.Change, RateThreshold);

            if (scoring > 0 && finishing < 0)
            {
                insights.Add("Dein Scoring hat sich verbessert, deine Checkout-Quote ist aber gesunken. Konzentriere dich auf dein Finish.");
            }
            else if (scoring < 0 && finishing > 0)
            {
                insights.Add("Dein Finish ist stärker geworden, dein Scoring aber schwächer. Übe die Aufnahmen auf T20 und T19.");
            }
            else if (scoring > 0 && finishing > 0)
            {
                insights.Add("Scoring und Finish haben sich beide verbessert. Weiter so!");
            }
            else if (scoring < 0 && finishing < 0)
            {
                insights.Add("Scoring und Finish sind im Vergleich zum Vorzeitraum gesunken.");
            }
            else if (scoring != 0)
            {
                insights.Add(scoring > 0 ? "Dein 3-Dart-Average ist deutlich gestiegen." : "Dein 3-Dart-Average ist deutlich gesunken.");
            }
            else if (finishing != 0)
            {
                insights.Add(finishing > 0 ? "Deine Checkout-Quote ist deutlich gestiegen." : "Deine Checkout-Quote ist deutlich gesunken.");
            }

            // Start in die Legs: First 9 entwickelt sich anders als der Gesamt-Average.
            if (start < 0 && scoring >= 0)
            {
                insights.Add("Dein Start in die Legs (First 9) ist schwächer geworden. Die ersten drei Aufnahmen setzen den Rest des Legs.");
            }
            else if (start > 0 && scoring <= 0)
            {
                insights.Add("Du startest stärker in die Legs (First 9), verlierst aber später Punkte. Achte auf die Aufnahmen im Mittelteil.");
            }

            return insights;
        }

        private static int Trend(decimal? change, decimal threshold) =>
            change is not { } c ? 0 : c >= threshold ? 1 : c <= -threshold ? -1 : 0;
    }
}
