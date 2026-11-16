using Shared.Models.Progression;

namespace Shared.Interfaces.Progression
{
    /// <summary>
    /// Level, Erfolge und Rekorde. Ausgewertet wird immer der ganze Verlauf (idempotent): nach jedem beendeten Spiel
    /// und beim Abruf des Fortschritts. So zählen auch Trainings, ohne dass jede Trainingsart einzeln melden muss.
    /// </summary>
    public interface IProgressionService
    {
        /// <summary>Wertet den Verlauf des Spielers aus, speichert Level und neu freigeschaltete Erfolge. Liefert die neuen Erfolge.</summary>
        Task<List<AchievementItem>> EvaluateAsync(int userId);

        /// <summary>Fortschritt des angemeldeten Spielers (wertet vorher aus, markiert neue Erfolge als gesehen).</summary>
        Task<PlayerProgress> GetMyProgressAsync();
    }
}
