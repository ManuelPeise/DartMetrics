using Shared.Enums.Logger;
using Shared.Models.Administration.Log;

namespace Shared.Interfaces.Administration
{
    public interface ILogService
    {
        Task<IEnumerable<LogmessageExportModel>> GetLogMessages(string? moduleFilter = null, LogMessageTypeEnum? messageTypeFilter = null);
        Task<IEnumerable<LogmessageExportModel>> DeleteMessage(int messageId);
        Task<IEnumerable<LogmessageExportModel>> DeleteMessages(IEnumerable<int> messageIds);
    }
}
