using Data.DartContext.Entities.Bot;
using Logic.Bot.Simulation;
using Shared.Enums.Bot;
using Shared.Models.Bot;

namespace Logic.Bot.Administration
{
    /// <summary>
    /// Steckbrief eines neuen Bots aus dem Basis-Ø: Streuung aus der Kalibrierung, dazu zufällige Eigenheiten
    /// (Neigung, senkrechte Streuung, Form, Nerven), damit zwei Bots gleicher Stärke verschieden spielen.
    /// Stärkere Bots haben weniger schlechte Darts und kennen die Checkout-Wege besser.
    /// </summary>
    public static class BotProfileFactory
    {
        /// <param name="randomize">false = Mittelwerte ohne Zufall (Kalibrierung).</param>
        /// <param name="sigmaOverride">Streuung vorgeben statt aus der Kalibrierung (nur für die Kalibrierung selbst).</param>
        public static DartBotProfileVersionEntity Create(float baseAverage, DartBotPlayingStyleEnum? style, Random random, bool randomize = true, float? sigmaOverride = null)
        {
            var average = Math.Clamp(baseAverage, DartBotStages.MinAverage, DartBotStages.MaxAverage);
            Func<float, float, float> around = randomize ? (center, spread) => Around(random, center, spread) : (center, _) => center;

            // 0 bei Ø 25, 1 bei Ø 105
            var strength = Math.Clamp((average - 25f) / 80f, 0f, 1f);

            return new DartBotProfileVersionEntity
            {
                VersionNumber = 1,
                BaseAverage = average,
                SigmaMm = sigmaOverride ?? AverageCalibration.SigmaForAverage(average),
                VerticalRatio = around(1.3f, 0.1f),
                BiasXMm = around(0f, 1f),
                BiasYMm = around(0f, 1f),
                BadDartRate = Math.Clamp(0.06f - 0.04f * strength + around(0f, 0.005f), 0.005f, 0.1f),
                // ThrowPrecision bleibt als grobe Einordnung (V1-Felder, Anzeige).
                ThrowPrecision = 0.15f + 0.8f * strength,
                ThrowConsistency = Math.Clamp(0.55f + 0.35f * strength + around(0f, 0.08f), 0f, 1f),
                TacticalIntelligence = Math.Clamp(0.35f + 0.65f * strength + around(0f, 0.05f), 0f, 1f),
                PressureResistance = Math.Clamp(0.4f + 0.45f * strength + around(0f, 0.12f), 0f, 1f),
                Nervousness = Math.Clamp(0.6f - 0.35f * strength + around(0f, 0.15f), 0f, 1f),
                RiskTaking = style switch
                {
                    DartBotPlayingStyleEnum.Aggressive => 0.8f,
                    DartBotPlayingStyleEnum.Defensive => 0.2f,
                    _ => 0.5f
                },
                PlayingStyle = style,
                ChangeReason = DartBotProfileChangeReasonEnum.Initial,
                ReasonText = $"Startprofil {DartBotStages.NameOf(DartBotStages.StageFor(average))}, Ø {average:0.#}"
            };
        }

        /// <summary>Potenzial: 15–35 Punkte über dem Start, höchstens Weltmeister-Niveau plus etwas Luft.</summary>
        public static float Potential(float baseAverage, Random random) =>
            MathF.Min(DartBotStages.MaxAverage - 5f, baseAverage + 15f + (float)random.NextDouble() * 20f);

        /// <summary>Gleichverteilt um einen Mittelwert (± spread).</summary>
        private static float Around(Random random, float center, float spread) =>
            center + ((float)random.NextDouble() * 2f - 1f) * spread;
    }
}
