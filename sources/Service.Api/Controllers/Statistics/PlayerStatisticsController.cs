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
        public async Task<IActionResult> GetPlayerStatistics([FromQuery] StatisticsPeriodEnum period = StatisticsPeriodEnum.Last30Days)
        {
            return Ok(await _statisticsService.GetStatisticsAsync(period));
        }
    }
}
