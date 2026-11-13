using Shared.Enums.Logger;
using Shared.Models.Logger;

namespace Shared.Interfaces.Logger
{
    public interface IMessageLog
    {
        Task LogMessage(string message, string module, Exception exception, LogMessageTypeEnum logMessageType = LogMessageTypeEnum.Error);
        Task<IEnumerable<LogMessage>> GetLogMessages(LogMessageTypeEnum? messageType = null);
        Task<bool> DeleteMessages(IEnumerable<int> messageIds);
    }
}
