using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums.User;
using Shared.Interfaces.Administration;

namespace Service.Api.Controllers.Administration
{
    [Authorize(Roles = nameof(UserRoleEnum.Admin))]
    public class MessageLogController: ApiControllerBase
    {
        private readonly ILogService _logService;

        public MessageLogController(ILogService logService)
        {
            _logService = logService;
        }

        [HttpGet(Name = "GetMessageLogs")]
        public async Task<IActionResult> GetMessageLogs()
        {
            var logs = await _logService.GetLogMessages();
            
            return Ok(logs);
        }

        [HttpPost(Name = "DeleteLogMessage")]
        public async Task<IActionResult> DeleteLogMessage([FromQuery] int id)
        {
            var log = await _logService.DeleteMessage(id);

            if (log == null)
            {
                return NotFound();
            }

            return Ok(log);
        }


        [HttpPost(Name = "DeleteLogMessages")]
        public async Task<IActionResult> DeleteLogMessages([FromBody] IEnumerable<int> ids)
        {
            var logs = await _logService.DeleteMessages(ids);

            return Ok(logs);
        }
    }
}
