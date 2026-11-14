using Microsoft.AspNetCore.Mvc;
using Shared.Interfaces.Administration;

namespace Service.Api.Controllers.Administration
{
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
        public async Task<IActionResult> DeleteLogMessages([FromRoute] IEnumerable<int> ids)
        {
            var log = _logService.DeleteMessages(ids);

            if (log == null)
            {
                return NotFound();
            }

            return Ok(log);
        }
    }
}
