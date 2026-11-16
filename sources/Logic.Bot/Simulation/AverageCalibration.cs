namespace Logic.Bot.Simulation
{
    /// <summary>
    /// Streuung (mm, waagerecht) ↔ 3-Dart-Durchschnitt, gemessen mit „BotTrainer calibrate“ (2000 Legs je Punkt,
    /// Wegebuch + Tabelle, typisches Profil: senkrecht ×1,3, 3 % schlechte Darts, Konstanz/Druck 0,8).
    /// Zwischen den Punkten logarithmisch interpoliert. Neu messen, wenn sich Wurfmodell oder Zielwahl ändern.
    /// </summary>
    public static class AverageCalibration
    {
        public static readonly (float Sigma, float Average)[] Points =
        [
            (3f, 126.8f), (4f, 110.9f), (5f, 100.5f), (6f, 91.0f), (7f, 83.7f), (8f, 76.9f), (10f, 65.9f), (12f, 56.9f),
            (15f, 45.8f), (18f, 38.7f), (22f, 32.4f), (27f, 26.3f), (33f, 21.9f), (40f, 18.7f), (50f, 15.5f), (60f, 13.8f), (75f, 12.1f)
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
