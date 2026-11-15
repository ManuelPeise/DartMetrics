using Data.DartContext.Entities.User;
using Logic.Shared;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Bot;
using Shared.Enums.Logger;
using Shared.Enums.User;
using Shared.Interfaces.Administration;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.UnitsOfWork;
using Shared.Interfaces.User;
using Shared.Models.Administration.User;
using Shared.Models.Authentication;

namespace Logic.Administration
{
    public class UserAdministration : ALogicBase, IUserAdministration
    {
        /// <summary>Rollen, die ein Admin vergeben darf. Admin ist bewusst nicht enthalten.</summary>
        private static readonly UserRoleEnum[] AssignableRoles = [UserRoleEnum.Player, UserRoleEnum.GameMaster];

        private readonly IClaimsHelper _claimsHelper;
        private readonly IUserAnonymizer _userAnonymizer;

        public UserAdministration(IUnitOfWork unitOfWork, IMessageLog messageLog, IClaimsHelper claimsHelper, IUserAnonymizer userAnonymizer)
            : base(unitOfWork, messageLog)
        {
            _claimsHelper = claimsHelper;
            _userAnonymizer = userAnonymizer;
        }

        public async Task<List<AdminUserListItem>> GetUsersAsync()
        {
            try
            {
                var admin = GetCurrentAdmin();

                var users = await UnitOfWork.UserUnitOfWork.UserRepository.GetWhereAsync(
                    u => u.DeletedAt == null,
                    asNoTracking: true,
                    include: q => q.Include(u => u.UserRoles).ThenInclude(ur => ur.Role));

                return users
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .Select(u =>
                    {
                        var role = GetRole(u);
                        var isSelf = u.Id == admin.UserId;
                        var isAdmin = role == UserRoleEnum.Admin;

                        return new AdminUserListItem
                        {
                            Id = u.Id,
                            FirstName = u.FirstName,
                            LastName = u.LastName,
                            DisplayName = u.DisplayName,
                            EmailAddress = u.EmailAddress,
                            Role = role,
                            IsActive = u.IsActive,
                            IsEmailVerified = u.EmailVerifiedAt != null,
                            CreatedAt = u.CreatedAt,
                            IsCurrentUser = isSelf,
                            CanChangeRole = !isSelf && !isAdmin,
                            CanManage = !isSelf && !isAdmin
                        };
                    })
                    .ToList();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error loading users.", nameof(UserAdministration), exception);

                throw;
            }
        }

        public async Task ChangeRoleAsync(ChangeUserRoleModel model)
        {
            try
            {
                var admin = GetCurrentAdmin();

                if (!AssignableRoles.Contains(model.Role))
                {
                    throw new ArgumentException("Only the roles Player and GameMaster can be assigned.");
                }

                var user = await LoadManageableUserAsync(model.UserId, admin);

                var newRole = await UnitOfWork.UserUnitOfWork.RoleRepository.GetFirstOrDefaultAsync(r => r.UserRole == model.Role)
                    ?? throw new InvalidOperationException($"Role {model.Role} does not exist.");

                // Genau eine Rolle je Benutzer: vorhandene Zuordnung umhängen, überzählige entfernen.
                var assignments = user.UserRoles.ToList();

                if (assignments.Count == 1 && assignments[0].RoleId == newRole.Id)
                {
                    return;
                }

                if (assignments.Count == 0)
                {
                    user.UserRoles.Add(new UserRoleEntity { RoleId = newRole.Id });
                }
                else
                {
                    assignments[0].RoleId = newRole.Id;
                    assignments[0].Role = newRole;

                    foreach (var extra in assignments.Skip(1))
                    {
                        UnitOfWork.UserUnitOfWork.UserRoleRepository.Delete(extra);
                    }
                }

                await UnitOfWork.SaveChangesAsync();

                await MessageLog.LogMessage($"Role of user {user.Id} changed to {model.Role} by user {admin.UserId}.", nameof(UserAdministration), null!, LogMessageTypeEnum.Information);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error changing role of user {model.UserId}.", nameof(UserAdministration), exception);

                throw;
            }
        }

        public Task DeactivateUserAsync(int userId) => SetActiveAsync(userId, false);

        public Task ActivateUserAsync(int userId) => SetActiveAsync(userId, true);

        public async Task DeleteUserAsync(int userId)
        {
            try
            {
                var admin = GetCurrentAdmin();
                var user = await LoadManageableUserAsync(userId, admin, includeCredentials: true);

                // Gemeinsame Anonymisierung (auch für das Löschen durch den Benutzer selbst), inkl. Turniere.
                await _userAnonymizer.AnonymizeAsync(user);

                await MessageLog.LogMessage($"User {user.Id} deleted (anonymized) by user {admin.UserId}.", nameof(UserAdministration), null!, LogMessageTypeEnum.Information);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error deleting user {userId}.", nameof(UserAdministration), exception);

                throw;
            }
        }

        private async Task SetActiveAsync(int userId, bool isActive)
        {
            try
            {
                var admin = GetCurrentAdmin();
                var user = await LoadManageableUserAsync(userId, admin);

                if (user.IsActive == isActive)
                {
                    return;
                }

                user.IsActive = isActive;

                await UnitOfWork.SaveChangesAsync();

                await MessageLog.LogMessage(
                    $"User {user.Id} {(isActive ? "activated" : "deactivated")} by user {admin.UserId}.",
                    nameof(UserAdministration), null!, LogMessageTypeEnum.Information);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error setting active state of user {userId}.", nameof(UserAdministration), exception);

                throw;
            }
        }

        /// <summary>
        /// Lädt einen Benutzer, den der Admin ändern darf: nicht er selbst, kein Admin, nicht gelöscht.
        /// </summary>
        private async Task<UserEntity> LoadManageableUserAsync(int userId, CurrentUser admin, bool includeCredentials = false)
        {
            if (userId == admin.UserId)
            {
                throw new UnauthorizedAccessException("Administrators cannot change their own account.");
            }

            var user = await UnitOfWork.UserUnitOfWork.UserRepository.GetFirstOrDefaultAsync(
                u => u.Id == userId && u.DeletedAt == null,
                include: q => includeCredentials
                    ? q.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).Include(u => u.UserCredentials)
                    : q.Include(u => u.UserRoles).ThenInclude(ur => ur.Role))
                ?? throw new KeyNotFoundException($"User with id {userId} was not found.");

            if (GetRole(user) == UserRoleEnum.Admin)
            {
                throw new UnauthorizedAccessException("Administrator accounts cannot be changed, deactivated or deleted.");
            }

            return user;
        }

        private CurrentUser GetCurrentAdmin()
        {
            var currentUser = _claimsHelper.GetCurrentUserFromClaims();

            if (currentUser == null || currentUser.UserId == 0)
            {
                throw new UnauthorizedAccessException("Current user not found.");
            }

            if (currentUser.Roles == null || !currentUser.Roles.Contains(UserRoleEnum.Admin))
            {
                throw new UnauthorizedAccessException("Only administrators can manage users.");
            }

            return currentUser;
        }

        /// <summary>Admin hat Vorrang, falls Altdaten mehrere Rollen enthalten (dann gilt der Benutzer als Admin).</summary>
        private static UserRoleEnum? GetRole(UserEntity user)
        {
            var roles = user.UserRoles
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role.UserRole)
                .ToList();

            if (roles.Count == 0)
            {
                return null;
            }

            return roles.Contains(UserRoleEnum.Admin) ? UserRoleEnum.Admin : roles[0];
        }
    }
}
