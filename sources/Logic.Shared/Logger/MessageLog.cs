using Data.DartContext;
using Data.DartContext.Entities.Log;
using Shared.Enums.Logger;
using Shared.Interfaces.Logger;
using Shared.Models.Logger;

namespace Logic.Shared.Logger
{
    public class MessageLog : IMessageLog
    {
        private readonly DartDatabaseContext _dartDatabaseContext;

        public MessageLog(DartDatabaseContext dartDatabaseContext)
        {
            _dartDatabaseContext = dartDatabaseContext;
        }

        public async Task LogMessage(string message, string module, Exception exception, LogMessageTypeEnum logMessageType = LogMessageTypeEnum.Error)
        {
            var logMessageEntity = new LogMessageEntity
            {
                Message = message,
                Module = module,
                ExceptionMessage = exception?.Message,
                Stacktrace = exception?.StackTrace,
                LogMessageType = logMessageType
            };

            _dartDatabaseContext.LogMessages.Add(logMessageEntity);

            await _dartDatabaseContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<LogMessage>> GetLogMessages(LogMessageTypeEnum? messageType = null)
        {
            var entities = _dartDatabaseContext.LogMessages.AsQueryable();

            if (messageType != null)
            {
                entities = entities.Where(x => x.LogMessageType == messageType);
            }

            return entities.Select(msg => new LogMessage
            {
                Message = msg.Message,
                Module = msg.Module,
                ExceptionMessage = msg.ExceptionMessage,
                Stacktrace = msg.Stacktrace,
                LogMessageType = msg.LogMessageType
            });
        }

        public async Task DeleteMessages(IEnumerable<int> messageIds)
        {
            var messagesToDelete = _dartDatabaseContext.LogMessages.Where(msg => messageIds.Contains(msg.Id));

            _dartDatabaseContext.LogMessages.RemoveRange(messagesToDelete);

            await _dartDatabaseContext.SaveChangesAsync();
        }
    }
}
