using Shared.Models.Authentication;
using Shared.Models.User;

namespace Shared.Interfaces.User
{
    public interface IUserProfileService
    {
        Task<UserProfileModel> GetProfileData();
        Task<UserProfileModel> UpdateUserProfile(UserProfileModel model);
        Task ChangePassword(ChangePasswordRequest request);
    }
}
