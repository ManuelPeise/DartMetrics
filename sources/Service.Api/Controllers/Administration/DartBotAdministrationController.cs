using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums.User;
using Shared.Interfaces.Administration;
using Shared.Models.Bot;

namespace Service.Api.Controllers.Administration
{
    [Authorize(Roles = nameof(UserRoleEnum.Admin))]
    public class DartBotAdministrationController: ApiControllerBase
    {
        private readonly IDartBotAdministration _dartBotAdministration;

        public DartBotAdministrationController(IDartBotAdministration dartBotAdministration)
        {
            _dartBotAdministration = dartBotAdministration;
        }

        [HttpGet(Name = "GetDartBots")]
        public async Task<IActionResult> GetDartBots([FromQuery] bool includeInactive = false)
        {
            var bots = await _dartBotAdministration.GetBotsAsync(includeInactive);

            return Ok(bots);
        }

        [HttpGet(Name = "GetDartBot")]
        public async Task<IActionResult> GetDartBot([FromQuery] int botId)
        {
            var bot = await _dartBotAdministration.GetBotAsync(botId);

            return Ok(bot);
        }

        [HttpPost(Name = "CreateDartBot")]
        public async Task<IActionResult> CreateDartBot([FromBody] CreateDartBotModel model)
        {
            var botId = await _dartBotAdministration.CreateBotAsync(model);

            return Ok(botId);
        }

        [HttpPost(Name = "ActivateDartBot")]
        public async Task<IActionResult> ActivateDartBot([FromQuery] int botId)
        {
            await _dartBotAdministration.ActivateBotAsync(botId);

            return Ok();
        }

        [HttpPost(Name = "DeactivateDartBot")]
        public async Task<IActionResult> DeactivateDartBot([FromQuery] int botId)
        {
            await _dartBotAdministration.DeactivateBotAsync(botId);

            return Ok();
        }
    }
}
