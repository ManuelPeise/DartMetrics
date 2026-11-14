using Shared.Enums.Logger;
using Shared.Models.Administration.Log;

namespace Shared.Interfaces.Administration
{
    public interface ILogService
    {
        Task<List<LogmessageExportModel>> GetLogMessages();
        Task<List<LogmessageExportModel>> DeleteMessage(int messageId);
        Task<List<LogmessageExportModel>> DeleteMessages(IEnumerable<int> messageIds);
    }
}
