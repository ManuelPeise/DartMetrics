using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums.User;
using Shared.Interfaces.Administration;
using Shared.Models.Bot;

namespace Service.Api.Controllers.Administration
{
    [Authorize(Roles = nameof(UserRoleEnum.Admin))]
    public class DartBotTrainingJobAdministrationController : ApiControllerBase
    {
        private readonly IDartBotTrainingJobAdministration _jobs;

        public DartBotTrainingJobAdministrationController(IDartBotTrainingJobAdministration jobs)
        {
            _jobs = jobs;
        }

        [HttpGet(Name = "GetDartBotTrainingJobs")]
        public async Task<IActionResult> GetDartBotTrainingJobs([FromQuery] int take = 20)
        {
            return Ok(await _jobs.GetJobsAsync(take));
        }

        [HttpPost(Name = "CreateDartBotTrainingJob")]
        public async Task<IActionResult> CreateDartBotTrainingJob([FromBody] CreateDartBotTrainingJobModel model)
        {
            return Ok(await _jobs.CreateJobAsync(model));
        }

        [HttpPost(Name = "CancelDartBotTrainingJob")]
        public async Task<IActionResult> CancelDartBotTrainingJob([FromQuery] int jobId)
        {
            await _jobs.CancelJobAsync(jobId);

            return Ok();
        }
    }
}
