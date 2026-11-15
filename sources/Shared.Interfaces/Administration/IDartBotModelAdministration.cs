using Shared.Models.Bot;

namespace Shared.Interfaces.Administration
{
    /// <summary>ML-Modelle der DartBots ansehen und das aktive Zielwahl-Modell setzen (nur Administratoren).</summary>
    public interface IDartBotModelAdministration
    {
        Task<DartBotModelOverview> GetModelsAsync();

        /// <summary>Nur mit gültiger Datei (vorhanden, SHA-256, Schema). Gilt für neu gestartete Bot-Spiele.</summary>
        Task ActivateModelAsync(int modelId);

        /// <summary>Kein Zielwahl-Modell aktiv: neue Bot-Spiele sind regelbasiert.</summary>
        Task DeactivateTargetModelAsync();
    }
}
