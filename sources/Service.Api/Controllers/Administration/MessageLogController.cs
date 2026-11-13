using Microsoft.AspNetCore.Mvc;
using Shared.Enums.Logger;
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
        public IActionResult GetMessageLogs([FromQuery] string? moduleFilter = null, [FromQuery] LogMessageTypeEnum? messageType = null)
        {
            var logs = _logService.GetLogMessages(moduleFilter, messageType);
            
            return Ok(logs);
        }

        [HttpPost(Name = "DeleteLogMessage")]
        public IActionResult DeleteLogMessage([FromQuery] int id)
        {
            var log = _logService.DeleteMessage(id);

            if (log == null)
            {
                return NotFound();
            }

            return Ok(log);
        }


        [HttpPost(Name = "DeleteLogMessages")]
        public IActionResult DeleteLogMessages([FromRoute] IEnumerable<int> ids)
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
