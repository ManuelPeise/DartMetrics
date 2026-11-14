using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.User
{
    public class UserEntity : AEntityBase
    {
        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string EmailAddress { get; set; } = null!;

        public string? DisplayName { get; set; }

        [NotMapped]
        public string Alias =>
            !string.IsNullOrWhiteSpace(DisplayName)
                ? DisplayName
                : FirstName;

        public bool IsActive { get; set; } = true;

        public DateTime? EmailVerifiedAt { get; set; }

        /// <summary>
        /// Gesetzt, wenn das Konto gelöscht (anonymisiert) wurde. Der Datensatz bleibt erhalten,
        /// damit Trainings und Bot-Spiele für Statistik und ML bestehen bleiben.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        public UserCredentialsEntity? UserCredentials { get; set; }

        public ICollection<UserRoleEntity> UserRoles { get; set; } = [];
    }
}
