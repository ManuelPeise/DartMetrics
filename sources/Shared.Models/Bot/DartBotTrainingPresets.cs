using Shared.Enums.Bot;

namespace Shared.Models.Bot
{
    /// <summary>
    /// Parameter je Umfang. Standard entspricht deploy/train-model (20.000 Situationen ≈ 1,1 Mio. Zeilen);
    /// mit nur ~1.000 Situationen lernt das Modell zu wenig und ist im Vergleich deutlich schlechter als das Regelwerk.
    /// </summary>
    public static class DartBotTrainingPresets
    {
        public static (int Situations, int RolloutsPerCandidate, int CompareLegs) For(DartBotTrainingJobPresetEnum preset) => preset switch
        {
            DartBotTrainingJobPresetEnum.Quick => (3000, 50, 500),
            DartBotTrainingJobPresetEnum.Standard => (20000, 50, 2000),
            // Für über Nacht: doppelt so viele Situationen, mehr Rollouts, Prüfstand mit 5.000 Legs je Stufe.
            DartBotTrainingJobPresetEnum.Thorough => (40000, 75, 5000),
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown training preset.")
        };
    }
}
