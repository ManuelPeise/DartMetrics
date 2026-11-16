using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums.Statistics;
using Shared.Enums.User;
using Shared.Interfaces.Statistics;

namespace Service.Api.Controllers.Statistics
{
    /// <summary>Eigene Statistik des angemeldeten Spielers.</summary>
    [Authorize(Roles = nameof(UserRoleEnum.Player))]
    public class PlayerStatisticsController : ApiControllerBase
    {
        private readonly IPlayerStatisticsService _statisticsService;

        public PlayerStatisticsController(IPlayerStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        [HttpGet(Name = "GetPlayerStatistics")]
        public async Task<IActionResult> GetPlayerStatistics(
            [FromQuery] StatisticsCategoryEnum category = StatisticsCategoryEnum.Overall,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            return Ok(await _statisticsService.GetStatisticsAsync(category, ToUtc(from), ToUtc(to)));
        }

        /// <summary>Die Seite schickt UTC im ISO-Format ("…Z"); ohne Zone wird der Wert als UTC behandelt.</summary>
        private static DateTime? ToUtc(DateTime? value) => value switch
        {
            null => null,
            { Kind: DateTimeKind.Utc } v => v,
            { Kind: DateTimeKind.Local } v => v.ToUniversalTime(),
            { } v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
        };
    }
}
