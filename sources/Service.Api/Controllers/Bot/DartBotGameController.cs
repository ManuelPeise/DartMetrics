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

        /// <summary>Laufendes Spiel des Benutzers; 204 (kein Inhalt), wenn keines läuft.</summary>
        [HttpGet(Name = "GetRunningDartBotGame")]
        public async Task<IActionResult> GetRunningDartBotGame()
        {
            var state = await _dartBotGameService.GetRunningGameAsync();

            return state is null ? NoContent() : Ok(state);
        }

        [HttpGet(Name = "GetDartBotRatingHistory")]
        public async Task<IActionResult> GetDartBotRatingHistory([FromQuery] int take = 20)
        {
            return Ok(await _dartBotGameService.GetRatingHistoryAsync(take));
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
