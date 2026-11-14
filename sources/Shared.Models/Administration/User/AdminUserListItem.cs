using Shared.Enums.User;

namespace Shared.Models.Administration.User
{
    /// <summary>Benutzer aus Sicht der Administration (gelöschte Konten sind nicht enthalten).</summary>
    public class AdminUserListItem
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string? DisplayName { get; set; }

        public string EmailAddress { get; set; } = null!;

        /// <summary>Jeder Benutzer hat genau eine Rolle; null nur bei fehlerhaften Altdaten.</summary>
        public UserRoleEnum? Role { get; set; }

        public bool IsActive { get; set; }

        public bool IsEmailVerified { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>Ist das der angemeldete Administrator selbst? (Dann keine Aktionen erlaubt.)</summary>
        public bool IsCurrentUser { get; set; }

        /// <summary>Darf die Rolle geändert werden? (nicht man selbst, keine Admins)</summary>
        public bool CanChangeRole { get; set; }

        /// <summary>Darf deaktiviert, aktiviert oder gelöscht werden? (nicht man selbst, keine Admins)</summary>
        public bool CanManage { get; set; }
    }
}
