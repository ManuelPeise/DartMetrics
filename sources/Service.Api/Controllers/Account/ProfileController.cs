using Microsoft.AspNetCore.Mvc;
using Shared.Interfaces.User;
using Shared.Models.Authentication;
using Shared.Models.User;
namespace Service.Api.Controllers.Account
{
    public class ProfileController : ApiControllerBase
    {
        private readonly IUserProfileService _profileService;

        public ProfileController(IUserProfileService userProfileService)
        {
            _profileService = userProfileService;
        }

        [HttpGet(Name = "GetProfile")]
        public async Task<UserProfileModel> GetProfile()
        {
            return await _profileService.GetProfileData();
        }

        [HttpPost(Name = "UpdateProfile")]
        public async Task<UserProfileModel> UpdateProfile([FromBody] UserProfileModel model)
        {
            return await _profileService.UpdateUserProfile(model);
        }

        [HttpPost(Name = "ChangePassword")]
        public async Task ChangePassword([FromBody] ChangePasswordRequest request)
        {
            await _profileService.ChangePassword(request);
        }
    }
}
