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
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown training preset.")
        };
    }
}
