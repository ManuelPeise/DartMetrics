namespace Shared.Enums.Bot
{
    /// <summary>Schritt eines Trainingsauftrags: Datensatz erzeugen, Modell trainieren, gegen Regelwerk vergleichen.</summary>
    public enum DartBotTrainingJobPhaseEnum
    {
        Generate = 1,
        Train = 2,
        Compare = 3
    }
}
