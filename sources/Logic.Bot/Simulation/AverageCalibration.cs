namespace Logic.Bot.Simulation
{
    /// <summary>
    /// Streuung (mm, waagerecht) ↔ 3-Dart-Durchschnitt, gemessen mit „BotTrainer calibrate“ (1500 Legs je Versuch, Bisektion):
    /// Bot wie bei der Erstellung (Mittelwerte je Stärke), mit Tagesform und Nerven, Zielwahl Wegebuch + Tabelle.
    /// Zwischen den Punkten logarithmisch interpoliert. Neu messen, wenn sich Wurfmodell oder Zielwahl ändern.
    /// </summary>
    public static class AverageCalibration
    {
        public static readonly (float Sigma, float Average)[] Points =
        [
            (3.47f, 114.7f), (3.82f, 109.7f), (4.28f, 105.1f), (4.78f, 99.8f), (5.35f, 94.6f), (5.89f, 90.2f), (6.58f, 84.6f),
            (7.60f, 77.2f), (8.78f, 69.5f), (9.98f, 61.8f), (11.34f, 55.5f), (13.31f, 47.1f), (15.37f, 39.8f), (17.19f, 35.1f),
            (19.85f, 30.3f), (23.29f, 24.7f), (29.14f, 20.2f), (40.77f, 15.1f)
        ];

        /// <summary>Streuung für einen Ziel-Durchschnitt (außerhalb der Messpunkte begrenzt).</summary>
        public static float SigmaForAverage(float average)
        {
            if (average >= Points[0].Average)
            {
                return Points[0].Sigma;
            }

            for (var i = 1; i < Points.Length; i++)
            {
                var (sigmaHigh, averageLow) = Points[i];
                var (sigmaLow, averageHigh) = Points[i - 1];

                if (average >= averageLow)
                {
                    // log(σ) linear in log(Ø)
                    var t = (MathF.Log(average) - MathF.Log(averageHigh)) / (MathF.Log(averageLow) - MathF.Log(averageHigh));
                    return MathF.Exp(MathF.Log(sigmaLow) + t * (MathF.Log(sigmaHigh) - MathF.Log(sigmaLow)));
                }
            }

            return Points[^1].Sigma;
        }

        /// <summary>Erwarteter Durchschnitt für eine Streuung (Umkehrung von SigmaForAverage).</summary>
        public static float AverageForSigma(float sigma)
        {
            if (sigma <= Points[0].Sigma)
            {
                return Points[0].Average;
            }

            for (var i = 1; i < Points.Length; i++)
            {
                if (sigma <= Points[i].Sigma)
                {
                    var t = (MathF.Log(sigma) - MathF.Log(Points[i - 1].Sigma)) / (MathF.Log(Points[i].Sigma) - MathF.Log(Points[i - 1].Sigma));
                    return MathF.Exp(MathF.Log(Points[i - 1].Average) + t * (MathF.Log(Points[i].Average) - MathF.Log(Points[i - 1].Average)));
                }
            }

            return Points[^1].Average;
        }
    }
}
