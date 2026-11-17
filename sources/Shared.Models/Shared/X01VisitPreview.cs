using Shared.Enums.X01;

namespace Shared.Models.Shared
{
    /// <summary>
    /// Vorschau einer X01-Aufnahme im Browser, bevor sie gesendet wird: zeigt Bust und Checkout sofort an
    /// und sperrt die Eingabe, wenn die Aufnahme sicher zu Ende ist.
    /// Rechnet wie <c>Logic.Shared.Rules.X01Rules</c> (Double-In, Double-/Straight-Out, Rest 1 bei Double-Out = Bust);
    /// gewertet wird weiterhin ausschließlich auf dem Server. Bei Regeländerungen beide Stellen anpassen.
    /// </summary>
    public static class X01VisitPreview
    {
        public const int DartsPerVisit = 3;

        /// <param name="remaining">Rest vor den Darts (bereits gewertete Darts der Aufnahme sind abgezogen).</param>
        /// <param name="isOpened">Bei Double-In: schon eröffnet?</param>
        /// <param name="dartsAlreadyThrown">Darts dieser Aufnahme, die der Server schon kennt (0–2).</param>
        /// <param name="darts">Noch nicht gesendete Darts als (Segment, Multiplier).</param>
        public static X01VisitPreviewResult Evaluate(
            int remaining,
            bool isOpened,
            X01InModeEnum inMode,
            X01OutModeEnum outMode,
            int dartsAlreadyThrown,
            IEnumerable<(int Segment, int Multiplier)> darts)
        {
            var opened = isOpened || inMode == X01InModeEnum.Straight;
            var rest = remaining;
            var count = dartsAlreadyThrown;

            foreach (var (segment, multiplier) in darts)
            {
                count++;

                if (!opened && (inMode == X01InModeEnum.Straight || IsDouble(segment, multiplier)))
                {
                    opened = true;
                }

                if (!opened)
                {
                    continue;
                }

                var next = rest - segment * multiplier;

                if (next == 0 && IsFinishingDart(segment, multiplier, outMode))
                {
                    return new X01VisitPreviewResult(0, opened, IsBust: false, IsCheckout: true, IsVisitFinished: true);
                }

                if (next < 0 || next == 0 || (next == 1 && outMode == X01OutModeEnum.Double))
                {
                    // Bust: der Rest springt auf den Stand vor der Aufnahme zurück (Anzeige zeigt den Rest davor).
                    return new X01VisitPreviewResult(remaining, opened, IsBust: true, IsCheckout: false, IsVisitFinished: true);
                }

                rest = next;
            }

            return new X01VisitPreviewResult(rest, opened, IsBust: false, IsCheckout: false, IsVisitFinished: count >= DartsPerVisit);
        }

        private static bool IsDouble(int segment, int multiplier) =>
            multiplier == 2 && (segment is >= 1 and <= 20 || segment == 25);

        private static bool IsFinishingDart(int segment, int multiplier, X01OutModeEnum outMode) =>
            outMode == X01OutModeEnum.Straight ? segment != 0 : IsDouble(segment, multiplier);
    }

    /// <summary>Ergebnis der Vorschau. Remaining ist bei Bust der Rest vor den gesendeten Darts.</summary>
    public readonly record struct X01VisitPreviewResult(int Remaining, bool IsOpened, bool IsBust, bool IsCheckout, bool IsVisitFinished);
}
