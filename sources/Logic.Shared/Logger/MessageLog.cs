using Data.DartContext;
using Data.DartContext.Entities.Log;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Enums.Logger;
using Shared.Interfaces.Logger;
using Shared.Models.Logger;
using System.Text;

namespace Logic.Shared.Logger
{
    public class MessageLog : IMessageLog
    {
        private const int MaxMessageLength = 1000;
        private const int MaxModuleLength = 200;

        private readonly DartDatabaseContext _dartDatabaseContext;
        private readonly ILogger<MessageLog> _logger;

        public MessageLog(DartDatabaseContext dartDatabaseContext, ILogger<MessageLog> logger)
        {
            _dartDatabaseContext = dartDatabaseContext;
            _logger = logger;
        }

        /// <summary>
        /// Writes a log entry to the database. Never throws: logging is called from catch blocks,
        /// an exception here would hide the original error.
        /// </summary>
        public async Task LogMessage(string message, string module, Exception exception, LogMessageTypeEnum logMessageType = LogMessageTypeEnum.Error)
        {
            // Always log to the normal .NET logger as well (console / debug output).
            _logger.Log(ToLogLevel(logMessageType), exception, "[{Module}] {Message}", module, message);

            try
            {
                // The DbContext is shared with the business logic. If its SaveChanges just failed, the faulty
                // entities are still tracked and would be saved again together with the log entry -> clear first.
                _dartDatabaseContext.ChangeTracker.Clear();

                var logMessageEntity = new LogMessageEntity
                {
                    Message = Truncate(message, MaxMessageLength),
                    Module = Truncate(module, MaxModuleLength),
                    ExceptionMessage = GetFullExceptionMessage(exception),
                    Stacktrace = exception?.ToString(),
                    LogMessageType = logMessageType,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                };

                _dartDatabaseContext.LogMessages.Add(logMessageEntity);

                await _dartDatabaseContext.SaveChangesAsync();
            }
            catch (Exception loggingException)
            {
                _dartDatabaseContext.ChangeTracker.Clear();
                _logger.LogError(loggingException, "Could not write log message to the database.");
            }
        }

        public async Task<IEnumerable<LogMessage>> GetLogMessages(LogMessageTypeEnum? messageType = null)
        {
            var entities = _dartDatabaseContext.LogMessages.AsNoTracking();

            if (messageType != null)
            {
                entities = entities.Where(x => x.LogMessageType == messageType);
            }

            return await entities
                .OrderByDescending(msg => msg.CreatedAt)
                .Select(msg => new LogMessage
                {
                    Message = msg.Message,
                    Module = msg.Module,
                    ExceptionMessage = msg.ExceptionMessage,
                    Stacktrace = msg.Stacktrace,
                    LogMessageType = msg.LogMessageType
                })
                .ToListAsync();
        }

        public async Task DeleteMessages(IEnumerable<int> messageIds)
        {
            var messagesToDelete = _dartDatabaseContext.LogMessages.Where(msg => messageIds.Contains(msg.Id));

            _dartDatabaseContext.LogMessages.RemoveRange(messagesToDelete);

            await _dartDatabaseContext.SaveChangesAsync();
        }

        /// <summary>
        /// EF providers wrap the real database error (e.g. "Table doesn't exist", "Incorrect datetime value")
        /// in InnerException. Collect the whole chain so the actual cause is visible.
        /// </summary>
        private static string? GetFullExceptionMessage(Exception? exception)
        {
            if (exception == null)
                return null;

            var builder = new StringBuilder();

            for (var current = exception; current != null; current = current.InnerException)
            {
                if (builder.Length > 0)
                    builder.Append(" ---> ");

                builder.Append(current.GetType().Name).Append(": ").Append(current.Message);
            }

            return builder.ToString();
        }

        private static string Truncate(string? value, int maxLength) =>
            string.IsNullOrEmpty(value) ? string.Empty : value.Length <= maxLength ? value : value[..maxLength];

        private static LogLevel ToLogLevel(LogMessageTypeEnum type) =>
            Enum.GetName(type) switch
            {
                "Error" => LogLevel.Error,
                "Warning" => LogLevel.Warning,
                "Debug" => LogLevel.Debug,
                _ => LogLevel.Information
            };
    }
}
