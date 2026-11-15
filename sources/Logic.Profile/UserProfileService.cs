using Data.DartContext.Security;
using Logic.Shared;
using Microsoft.EntityFrameworkCore;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.UnitsOfWork;
using Shared.Interfaces.User;
using Shared.Models.Authentication;
using Shared.Models.User;
using Data.DartContext.Entities.User;

namespace Logic.Profile
{
    public class UserProfileService : ALogicBase, IUserProfileService
    {
        private readonly IClaimsHelper _claimsHelper;
        private readonly IUserAnonymizer _userAnonymizer;

        public UserProfileService(IUnitOfWork unitOfWork, IMessageLog messageLog, IClaimsHelper claimsHelper, IUserAnonymizer userAnonymizer) : base(unitOfWork, messageLog)
        {
            _userAnonymizer = userAnonymizer;
            _claimsHelper = claimsHelper;
        }

        public async Task<UserProfileModel> GetProfileData()
        {
            try
            {
                var currentUser = _claimsHelper.GetCurrentUserFromClaims();

                if(currentUser == null)
                {
                    throw new ArgumentException("Current user is not authenticated.");
                }

                var userEntity = await UnitOfWork.UserUnitOfWork.UserRepository.GetFirstOrDefaultAsync(u => u.Id == currentUser.UserId);
                
                if(userEntity == null)
                {
                    throw new KeyNotFoundException("User not found in database.");
                }

                return new UserProfileModel
                {
                    UserId = userEntity.Id,
                    FirstName = userEntity.FirstName,
                    LastName = userEntity.LastName,
                    DisplayName = userEntity.DisplayName,
                    EmailAddress = userEntity.EmailAddress,
                    IsVerified = userEntity.EmailVerifiedAt.HasValue,
                    Rating = userEntity.Rating,
                    AvatarVersion = userEntity.AvatarVersion?.ToString("N")
                };
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("An error occurred while getting profile data.", nameof(UserProfileService), exception);

                throw;
            }
        }

        public async Task<UserProfileModel> UpdateUserProfile(UserProfileModel model)
        {
            try
            {
                var currentUser = _claimsHelper.GetCurrentUserFromClaims();

                EnsureModelNotNull(currentUser, "Could not load current user.");
               
                var userEntity = await UnitOfWork.UserUnitOfWork.UserRepository.GetByIdAsync(currentUser.UserId);

                EnsureModelNotNull(userEntity, "Could not find user in database.");

                userEntity.FirstName = RequiredName(model.FirstName, nameof(model.FirstName));
                userEntity.LastName = RequiredName(model.LastName, nameof(model.LastName));
                userEntity.DisplayName = string.IsNullOrWhiteSpace(model.DisplayName)
                    ? null
                    : LimitedName(model.DisplayName.Trim(), nameof(model.DisplayName));

                await UnitOfWork.SaveChangesAsync();

                return new UserProfileModel
                {
                    UserId = userEntity.Id,
                    FirstName = userEntity.FirstName,
                    LastName = userEntity.LastName,
                    DisplayName = userEntity.DisplayName,
                    EmailAddress = userEntity.EmailAddress,
                    IsVerified = userEntity.EmailVerifiedAt.HasValue,
                    Rating = userEntity.Rating
                };

            }catch(Exception exception)
            {
                await MessageLog.LogMessage("Update user profile failed", nameof(UserProfileService), exception);
                throw;
            }
        }

        public async Task DeleteOwnAccount(DeleteOwnAccountModel model)
        {
            try
            {
                ArgumentException.ThrowIfNullOrEmpty(model.Password, nameof(model.Password));

                var currentUser = _claimsHelper.GetCurrentUserFromClaims();

                if (currentUser == null || currentUser.UserId == 0)
                {
                    throw new UnauthorizedAccessException("Current user is not authenticated.");
                }

                if (currentUser.Roles?.Contains(global::Shared.Enums.User.UserRoleEnum.Admin) == true)
                {
                    throw new UnauthorizedAccessException("Administrators cannot delete their own account.");
                }

                var userEntity = await UnitOfWork.UserUnitOfWork.UserRepository.GetFirstOrDefaultAsync(
                    u => u.Id == currentUser.UserId && u.DeletedAt == null,
                    include: x => x.Include(u => u.UserCredentials));

                if (userEntity?.UserCredentials == null)
                {
                    throw new ArgumentException("User not found in database.");
                }

                if (!PasswordHasher.VerifyPassword(model.Password, userEntity.UserCredentials.PasswordHash))
                {
                    throw new ArgumentException("The password is incorrect.");
                }

                await _userAnonymizer.AnonymizeAsync(userEntity);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error deleting own account.", nameof(UserProfileService), exception);
                throw;
            }
        }

        public async Task ChangePassword(ChangePasswordRequest request)
        {
            try
            {
                ArgumentException.ThrowIfNullOrEmpty(request.CurrentPassword, nameof(request.CurrentPassword));
                ArgumentException.ThrowIfNullOrEmpty(request.Password, nameof(request.Password));
                ArgumentException.ThrowIfNullOrEmpty(request.PasswordReplication, nameof(request.PasswordReplication));

                if(!PasswordHasher.IsMatchingPassword(request.Password, request.PasswordReplication))
                {
                    throw new ArgumentException("Password and password replication do not match.");
                }

                PasswordPolicy.Validate(request.Password, nameof(request.Password));

                if (PasswordHasher.IsMatchingPassword(request.Password, request.CurrentPassword))
                {
                    throw new ArgumentException("The new password must differ from the current password.");
                }

                var currentUser = _claimsHelper.GetCurrentUserFromClaims();

                if(currentUser == null)
                {
                    throw new ArgumentException("Current user is not authenticated.");
                }

                var userEntity = await UnitOfWork.UserUnitOfWork.UserRepository.GetFirstOrDefaultAsync(u => u.Id == currentUser.UserId, include: x => x.Include(u => u.UserCredentials));
                
                if(userEntity == null || userEntity.UserCredentials == null)
                {
                    throw new ArgumentException("User not found in database.");
                }

                // BCrypt hasht mit Salt: vergleichen nur über Verify, nie über einen neuen Hash.
                if (!PasswordHasher.VerifyPassword(request.CurrentPassword, userEntity.UserCredentials.PasswordHash))
                {
                    throw new ArgumentException("Could not change password because the current password is incorrect.");
                }

                userEntity.UserCredentials.PasswordHash = PasswordHasher.HashPassword(request.Password);
                userEntity.UserCredentials.PasswordChangedAt = DateTime.UtcNow;

                // Einmal-Passwort erledigt; das aktuelle Token trägt den Hinweis noch, daher meldet sich der Client danach neu an.
                userEntity.UserCredentials.MustChangePassword = false;

                await UnitOfWork.SaveChangesAsync();

            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("An error occurred while changing password.", nameof(UserProfileService), exception);

                throw;
            }
        }

        public async Task<string> UploadAvatar(UploadAvatarModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                var data = model?.Data ?? [];

                if (data.Length == 0 || data.Length > AvatarRules.MaxBytes)
                {
                    throw new ArgumentException($"The image must be between 1 byte and {AvatarRules.MaxBytes / 1024} KB.");
                }

                // Typ aus den Bytes bestimmen, nie aus Angaben des Clients.
                var contentType = DetectImageType(data)
                    ?? throw new ArgumentException("Only JPEG, PNG or WebP images are allowed.");

                var user = await UnitOfWork.UserUnitOfWork.UserRepository.GetByIdAsync(userId);
                var avatar = await UnitOfWork.UserUnitOfWork.UserAvatarRepository.GetFirstOrDefaultAsync(a => a.UserId == userId);

                if (avatar is null)
                {
                    avatar = new UserAvatarEntity { UserId = userId };
                    await UnitOfWork.UserUnitOfWork.UserAvatarRepository.AddAsync(avatar);
                }

                avatar.ContentType = contentType;
                avatar.Data = data;

                var version = Guid.NewGuid();
                user.AvatarVersion = version;

                await UnitOfWork.SaveChangesAsync();

                return version.ToString("N");
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("An error occurred while uploading the avatar.", nameof(UserProfileService), exception);
                throw;
            }
        }

        public async Task DeleteAvatar()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await UnitOfWork.UserUnitOfWork.UserRepository.GetByIdAsync(userId);
                var avatar = await UnitOfWork.UserUnitOfWork.UserAvatarRepository.GetFirstOrDefaultAsync(a => a.UserId == userId);

                if (avatar is not null)
                {
                    UnitOfWork.UserUnitOfWork.UserAvatarRepository.Delete(avatar);
                }

                user.AvatarVersion = null;

                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("An error occurred while deleting the avatar.", nameof(UserProfileService), exception);
                throw;
            }
        }

        public async Task<AvatarImage?> GetAvatar()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await UnitOfWork.UserUnitOfWork.UserRepository.GetByIdAsync(userId, asNoTracking: true);

                if (user.AvatarVersion is null)
                {
                    return null;
                }

                var avatar = await UnitOfWork.UserUnitOfWork.UserAvatarRepository.GetFirstOrDefaultAsync(a => a.UserId == userId, asNoTracking: true);

                return avatar is null
                    ? null
                    : new AvatarImage { ContentType = avatar.ContentType, Data = avatar.Data, Version = user.AvatarVersion.Value.ToString("N") };
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("An error occurred while loading the avatar.", nameof(UserProfileService), exception);
                throw;
            }
        }

        private int GetCurrentUserId()
        {
            var currentUser = _claimsHelper.GetCurrentUserFromClaims();

            if (currentUser == null || currentUser.UserId == 0)
            {
                throw new UnauthorizedAccessException("Current user is not authenticated.");
            }

            return currentUser.UserId;
        }

        /// <summary>JPEG, PNG oder WebP an den Signatur-Bytes erkennen.</summary>
        private static string? DetectImageType(byte[] data)
        {
            if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            {
                return "image/jpeg";
            }

            if (data.Length >= 8 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            {
                return "image/png";
            }

            if (data.Length >= 12 &&
                data.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                data.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            {
                return "image/webp";
            }

            return null;
        }

        private const int MaxNameLength = 100;

        private static string RequiredName(string? value, string field)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{field} is required.", field);
            }

            return LimitedName(value.Trim(), field);
        }

        private static string LimitedName(string value, string field) =>
            value.Length <= MaxNameLength
                ? value
                : throw new ArgumentException($"{field} must not exceed {MaxNameLength} characters.", field);

        private void EnsureModelNotNull<TModel>(TModel model, string error)
        {
            if(model == null)
            {
                throw new Exception(error);
            }
        }
    }
}
