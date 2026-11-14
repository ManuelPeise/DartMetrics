using Data.DartContext.Security;
using Logic.Shared;
using Microsoft.EntityFrameworkCore;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.UnitsOfWork;
using Shared.Interfaces.User;
using Shared.Models.Authentication;
using Shared.Models.User;

namespace Logic.Profile
{
    public class UserProfileService : ALogicBase, IUserProfileService
    {
        private readonly IClaimsHelper _claimsHelper;
        public UserProfileService(IUnitOfWork unitOfWork, IMessageLog messageLog, IClaimsHelper claimsHelper) : base(unitOfWork, messageLog)
        {
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
                
                if(userEntity == null || userEntity.UserCredentials == null)
                {
                    throw new ArgumentException("User not found in database.");
                }

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

                userEntity.FirstName = model.FirstName;
                userEntity.LastName = model.LastName;
                userEntity.DisplayName = model.DisplayName;

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

                if (!PasswordHasher.IsMatchingPassword(PasswordHasher.HashPassword(request.CurrentPassword), userEntity.UserCredentials.PasswordHash))
                {
                    throw new ArgumentException("Could not change password because the current password is incorrect.");
                }

                userEntity.UserCredentials.PasswordHash = PasswordHasher.HashPassword(request.Password);
                
                await UnitOfWork.SaveChangesAsync();

            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("An error occurred while changing password.", nameof(UserProfileService), exception);
            }
        }

        private void EnsureModelNotNull<TModel>(TModel model, string error)
        {
            if(model == null)
            {
                throw new Exception(error);
            }
        }
    }
}
