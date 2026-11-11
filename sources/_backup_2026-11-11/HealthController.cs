using Microsoft.AspNetCore.Mvc;

namespace Service.Api.Controllers.Health
{
    public class HealthController: ApiControllerBase
    {
        public HealthController()
        {
            
        }

        [HttpGet(Name = "GetHealth")]
        public async Task<IActionResult> GetHealth()
        {
            return Ok(new { status = "Healthy" });
        }
    }
}
