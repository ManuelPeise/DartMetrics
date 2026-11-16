using Data.DartContext.Entities.Bot;
using Logic.Bot.Extensions;
using Shared.Interfaces.Bot;

namespace Logic.Bot.Learning
{
    /// <summary>
    /// Form der Bots in drei Ebenen:
    /// 1. Formkurve über Spiele: heute = 70 % gestern + 30 % neuer Zufall (AR(1)), Stärke der Schwankung aus der Konstanz.
    /// 2. Tagesform je Spiel: Faktor exp(Formkurve) auf die Streuung, beim Spielstart gezogen und gespeichert.
    /// 3. Nerven im Spiel: verpasste Finish-Doppel und Busts seit dem letzten eigenen Checkout erhöhen die Streuung.
    /// Selbstvertrauen: nach Sieg/Niederlage verschiebt sich die Formkurve leicht.
    /// </summary>
    public static class BotForm
    {
        /// <summary>Wie viel von der letzten Form erhalten bleibt (0 = jeder Tag unabhängig).</summary>
        public const float Persistence = 0.7f;

        /// <summary>Schwankung der Formkurve bei Konstanz 0 (Standardabweichung des log. Faktors).</summary>
        public const float MaxDeviation = 0.25f;

        /// <summary>Verschiebung der Formkurve nach einem Sieg (besser) bzw. einer Niederlage (schlechter).</summary>
        public const float ConfidenceStep = 0.03f;

        /// <summary>Grenze der Formkurve (log.), damit kein Bot dauerhaft aus seiner Stufe fällt.</summary>
        public const float Limit = 0.5f;

        /// <summary>Streuung je Fehler-Ereignis bei Nervosität 1.</summary>
        public const float NervesPerEvent = 0.06f;

        public const int MaxNerveEvents = 4;

        /// <summary>Nächste Formkurve (log.) aus der letzten; stationäre Streuung = (1 − Konstanz) · MaxDeviation.</summary>
        public static float NextForm(float previous, float consistency, IRandomSource random)
        {
            var deviation = (1f - Math.Clamp(consistency, 0f, 1f)) * MaxDeviation;
            var innovation = MathF.Sqrt(1f - Persistence * Persistence) * deviation * (float)random.NextGaussian();

            return Math.Clamp(Persistence * previous + innovation, -Limit, Limit);
        }

        public static float DayFactor(float form) => MathF.Exp(form);

        public static float AfterResult(float form, bool won) => Math.Clamp(form + (won ? -ConfidenceStep : ConfidenceStep), -Limit, Limit);

        /// <summary>Faktor auf die Streuung durch Nerven: Fehler-Ereignisse seit dem letzten eigenen Checkout.</summary>
        public static float NervesFactor(IEnumerable<DartBotGameVisitEntity> ownVisitsInOrder, float nervousness)
        {
            var events = 0;

            foreach (var visit in ownVisitsInOrder)
            {
                if (visit.IsCheckout)
                {
                    events = 0;
                    continue;
                }

                if (visit.IsBust)
                {
                    events++;
                }

                events += visit.Throws.Count(IsMissedFinishingDouble);
            }

            return 1f + Math.Clamp(nervousness, 0f, 1f) * NervesPerEvent * Math.Min(events, MaxNerveEvents);
        }

        /// <summary>Auf ein Finish-Doppel (bzw. Bull) gezielt und nicht getroffen.</summary>
        private static bool IsMissedFinishingDouble(DartBotGameThrowEntity dart) =>
            dart.TargetMultiplier == 2 &&
            dart.TargetSegment is { } segment &&
            segment * 2 == dart.ScoreBeforeThrow &&
            (dart.Segment != segment || dart.Multiplier != 2);
    }
}
