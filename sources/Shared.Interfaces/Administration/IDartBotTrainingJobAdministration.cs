using Shared.Models.Bot;

namespace Shared.Interfaces.Administration
{
    /// <summary>Trainingsaufträge für Zielwahl-Modelle anlegen, ansehen und abbrechen (nur Administratoren).</summary>
    public interface IDartBotTrainingJobAdministration
    {
        /// <summary>Neueste zuerst, höchstens <paramref name="take"/>.</summary>
        Task<List<DartBotTrainingJobListItem>> GetJobsAsync(int take = 20);

        /// <summary>Es läuft bzw. wartet immer höchstens ein Auftrag.</summary>
        Task<int> CreateJobAsync(CreateDartBotTrainingJobModel model);

        /// <summary>Wartend: sofort abgebrochen. Laufend: der Worker bricht beim nächsten Zwischenstand ab.</summary>
        Task CancelJobAsync(int jobId);
    }
}
