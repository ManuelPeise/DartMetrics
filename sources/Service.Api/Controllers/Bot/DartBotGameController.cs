using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums.User;
using Shared.Interfaces.Bot;
using Shared.Models.Bot;

namespace Service.Api.Controllers.Bot
{
    [Authorize(Roles = nameof(UserRoleEnum.Player))]
    public class DartBotGameController: ApiControllerBase
    {
        private readonly IDartBotGameService _dartBotGameService;

        public DartBotGameController(IDartBotGameService dartBotGameService)
        {
            _dartBotGameService = dartBotGameService;
        }

        [HttpPost(Name = "StartDartBotGame")]
        public async Task<IActionResult> StartDartBotGame([FromBody] StartDartBotGameModel model)
        {
            var state = await _dartBotGameService.StartGameAsync(model);

            return Ok(state);
        }

        [HttpPost(Name = "AddDartBotGameVisit")]
        public async Task<IActionResult> AddDartBotGameVisit([FromBody] DartBotUserVisitModel model)
        {
            var state = await _dartBotGameService.AddUserVisitAsync(model);

            return Ok(state);
        }

        [HttpPost(Name = "AbortDartBotGame")]
        public async Task<IActionResult> AbortDartBotGame([FromQuery] int gameId)
        {
            await _dartBotGameService.AbortGameAsync(gameId);

            return Ok();
        }

        [HttpGet(Name = "GetAvailableDartBots")]
        public async Task<IActionResult> GetAvailableDartBots()
        {
            var bots = await _dartBotGameService.GetAvailableBotsAsync();

            return Ok(bots);
        }

        [HttpGet(Name = "GetDartBotGame")]
        public async Task<IActionResult> GetDartBotGame([FromQuery] int gameId)
        {
            var state = await _dartBotGameService.GetGameAsync(gameId);

            return Ok(state);
        }
    }
}
