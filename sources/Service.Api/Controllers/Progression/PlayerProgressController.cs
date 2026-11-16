using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums.User;
using Shared.Interfaces.Progression;

namespace Service.Api.Controllers.Progression
{
    /// <summary>Level, Erfolge und Rekorde des angemeldeten Spielers.</summary>
    [Authorize(Roles = nameof(UserRoleEnum.Player))]
    public class PlayerProgressController : ApiControllerBase
    {
        private readonly IProgressionService _progression;

        public PlayerProgressController(IProgressionService progression)
        {
            _progression = progression;
        }

        [HttpGet(Name = "GetMyProgress")]
        public async Task<IActionResult> GetMyProgress()
        {
            return Ok(await _progression.GetMyProgressAsync());
        }
    }
}
