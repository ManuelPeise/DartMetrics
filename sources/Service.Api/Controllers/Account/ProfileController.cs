using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums.User;
using Shared.Interfaces.User;
using Shared.Models.Authentication;
using Shared.Models.User;

namespace Service.Api.Controllers.Account
{
    [Authorize(Roles = $"{nameof(UserRoleEnum.Admin)}, {nameof(UserRoleEnum.Player)}, {nameof(UserRoleEnum.GameMaster)}")]
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

        [HttpPost(Name = "UploadAvatar")]
        [RequestSizeLimit(1024 * 1024)] // Base64 von höchstens AvatarRules.MaxBytes plus JSON
        public async Task<IActionResult> UploadAvatar([FromBody] UploadAvatarModel model)
        {
            // Als Objekt, nicht als nackter String (der würde als text/plain statt JSON geschrieben).
            return Ok(new UploadAvatarResult { Version = await _profileService.UploadAvatar(model) });
        }

        [HttpPost(Name = "DeleteAvatar")]
        public async Task<IActionResult> DeleteAvatar()
        {
            await _profileService.DeleteAvatar();

            return Ok();
        }

        /// <summary>Eigenes Profilbild. Die URL enthält ?v=Version, daher darf der Browser lange cachen.</summary>
        [HttpGet(Name = "GetAvatar")]
        public async Task<IActionResult> GetAvatar()
        {
            var avatar = await _profileService.GetAvatar();

            if (avatar is null)
            {
                return NotFound();
            }

            Response.Headers.CacheControl = "private, max-age=31536000, immutable";
            Response.Headers.XContentTypeOptions = "nosniff";

            return File(avatar.Data, avatar.ContentType);
        }

        [HttpPost(Name = "DeleteOwnAccount")]
        public async Task<IActionResult> DeleteOwnAccount([FromBody] DeleteOwnAccountModel model)
        {
            await _profileService.DeleteOwnAccount(model);
            return Ok();
        }

        [HttpPost(Name = "ChangePassword")]
        public async Task ChangePassword([FromBody] ChangePasswordRequest request)
        {
            await _profileService.ChangePassword(request);
        }
    }
}
