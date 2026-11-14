using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums.User;
using Shared.Interfaces.Administration;
using Shared.Models.Administration.User;

namespace Service.Api.Controllers.Administration
{
    /// <summary>Benutzerverwaltung, nur für Administratoren.</summary>
    [Authorize(Roles = nameof(UserRoleEnum.Admin))]
    public class UserAdministrationController : ApiControllerBase
    {
        private readonly IUserAdministration _userAdministration;

        public UserAdministrationController(IUserAdministration userAdministration)
        {
            _userAdministration = userAdministration;
        }

        [HttpGet(Name = "GetUsers")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userAdministration.GetUsersAsync();

            return Ok(users);
        }

        [HttpPost(Name = "ChangeUserRole")]
        public async Task<IActionResult> ChangeUserRole([FromBody] ChangeUserRoleModel model)
        {
            await _userAdministration.ChangeRoleAsync(model);

            return Ok();
        }

        [HttpPost(Name = "DeactivateUser")]
        public async Task<IActionResult> DeactivateUser([FromQuery] int userId)
        {
            await _userAdministration.DeactivateUserAsync(userId);

            return Ok();
        }

        [HttpPost(Name = "ActivateUser")]
        public async Task<IActionResult> ActivateUser([FromQuery] int userId)
        {
            await _userAdministration.ActivateUserAsync(userId);

            return Ok();
        }

        [HttpPost(Name = "DeleteUser")]
        public async Task<IActionResult> DeleteUser([FromQuery] int userId)
        {
            await _userAdministration.DeleteUserAsync(userId);

            return Ok();
        }
    }
}
