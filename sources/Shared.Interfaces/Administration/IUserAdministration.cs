using Shared.Models.Administration.User;

namespace Shared.Interfaces.Administration
{
    /// <summary>
    /// Benutzerverwaltung für Administratoren.
    /// Regeln: das eigene Konto ist gesperrt; Admin-Konten können nicht geändert, deaktiviert oder gelöscht werden;
    /// vergeben werden können nur die Rollen Player und GameMaster.
    /// </summary>
    public interface IUserAdministration
    {
        Task<List<AdminUserListItem>> GetUsersAsync();

        Task ChangeRoleAsync(ChangeUserRoleModel model);

        Task DeactivateUserAsync(int userId);

        Task ActivateUserAsync(int userId);

        /// <summary>Anonymisiert das Konto: Login-Daten weg, persönliche Daten ersetzt, Spiele bleiben erhalten.</summary>
        Task DeleteUserAsync(int userId);
    }
}
