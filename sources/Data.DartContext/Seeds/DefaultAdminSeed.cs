using Data.DartContext.Entities.User;
using Data.DartContext.Security;
using Shared.Models.Options;

namespace Data.DartContext.Seeds
{
    public class DefaultAdminSeed
    {
        private readonly AdminUserOptions _adminUserOptions;

        public DefaultAdminSeed(AdminUserOptions options)
        {
            _adminUserOptions = options;
        }

        public void Seed(DartDatabaseContext context)
        {
            if (context.Users.Any(u => u.EmailAddress == _adminUserOptions.EmailAddress))
            {
                return;
            }

            var utcNow = DateTime.UtcNow;

            var adminUser = new UserEntity
            {
                FirstName = _adminUserOptions.FirstName,
                LastName = _adminUserOptions.LastName,
                EmailAddress = _adminUserOptions.EmailAddress,
                EmailVerifiedAt = utcNow,
                IsActive = true,
                UserRoles = new List<UserRoleEntity>
                {
                    new UserRoleEntity
                    {
                        RoleId = 1,
                        CreatedAt = utcNow,
                        CreatedBy = "System",
                    }
                },
                UserCredentials = new UserCredentialsEntity
                {
                    PasswordHash = PasswordHasher.HashPassword(_adminUserOptions.Password),
                    PasswordChangedAt = utcNow,
                    // Das Passwort steht in der Konfiguration: beim ersten Login muss es geändert werden.
                    MustChangePassword = true,
                    CreatedAt = utcNow,
                    CreatedBy = "System",
                },
                CreatedAt = utcNow,
                CreatedBy = "System",
            };

            context.Users.Add(adminUser);
            context.SaveChanges();
        }
    }
}
