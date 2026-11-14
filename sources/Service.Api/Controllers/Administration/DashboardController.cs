using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums.User;
using Shared.Interfaces.Administration;

namespace Service.Api.Controllers.Administration
{
    /// <summary>Kennzahlen für das Admin-Dashboard, nur für Administratoren.</summary>
    [Authorize(Roles = nameof(UserRoleEnum.Admin))]
    public class DashboardController : ApiControllerBase
    {
        private readonly IDashboardStatisticsService _statisticsService;

        public DashboardController(IDashboardStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        [HttpGet(Name = "GetDashboardStatistics")]
        public async Task<IActionResult> GetDashboardStatistics()
        {
            return Ok(await _statisticsService.GetStatisticsAsync());
        }
    }
}
