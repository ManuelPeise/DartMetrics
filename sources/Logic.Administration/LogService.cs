using Logic.Shared;
using Shared.Enums.Logger;
using Shared.Enums.User;
using Shared.Interfaces.Administration;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Administration.Log;

namespace Logic.Administration
{
    public class LogService : ALogicBase, ILogService
    {
        private readonly IClaimsHelper claimsHelper;

        public LogService(IMessageLog messageLog, IUnitOfWork unitOfWork, IClaimsHelper claimsHelper) : base(unitOfWork, messageLog)
        {
            this.claimsHelper = claimsHelper;
        }

        public async Task<List<LogmessageExportModel>> GetLogMessages()
        {
            try
            {
                var currentUser = claimsHelper.GetCurrentUserFromClaims();

                if (currentUser == null || !currentUser.Roles.Contains(UserRoleEnum.Admin))
                {
                    throw new UnauthorizedAccessException("Current user is not authenticated.");
                }

                var messages = await MessageLog.GetLogMessages();

                var response = messages.Select(msg => new LogmessageExportModel
                {
                    Id = msg.Id,
                    Message = msg.Message,
                    Module = msg.Module,
                    MessageType = msg.LogMessageType,
                    ExceptionMessage = msg.ExceptionMessage,
                    Stacktrace = msg.Stacktrace,
                    CreatedAt = msg.CreatedAt
                }).ToList();

                return response;
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Could not retrieve log messages", nameof(LogService), exception, LogMessageTypeEnum.Error);

                throw;
            }
        }

        public async Task<List<LogmessageExportModel>> DeleteMessage(int messageId)
        {
            try
            {
                var currentUser = claimsHelper.GetCurrentUserFromClaims();

                if (currentUser == null || !currentUser.Roles.Contains(UserRoleEnum.Admin))
                {
                    throw new UnauthorizedAccessException("Current user is not authenticated.");
                }

                await MessageLog.DeleteMessages(new List<int> { messageId });

                var messages = await MessageLog.GetLogMessages(null);

                return messages.Select(msg => new LogmessageExportModel
                {
                    Id = msg.Id,
                    Message = msg.Message,
                    Module = msg.Module,
                    MessageType = msg.LogMessageType,
                    ExceptionMessage = msg.ExceptionMessage,
                    Stacktrace = msg.Stacktrace,
                    CreatedAt = msg.CreatedAt
                }).ToList();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Could not delete log message", nameof(LogService), exception, LogMessageTypeEnum.Error);

                throw;
            }
        }

        public async Task<List<LogmessageExportModel>> DeleteMessages(IEnumerable<int> messageIds)
        {
            try
            {
                var currentUser = claimsHelper.GetCurrentUserFromClaims();

                if (currentUser == null || !currentUser.Roles.Contains(UserRoleEnum.Admin))
                {
                    throw new UnauthorizedAccessException("Current user is not authenticated.");
                }

                await MessageLog.DeleteMessages(messageIds);

                var messages = await MessageLog.GetLogMessages(null);

                return messages.Select(msg => new LogmessageExportModel
                {
                    Id = msg.Id,
                    Message = msg.Message,
                    Module = msg.Module,
                    MessageType = msg.LogMessageType,
                    ExceptionMessage = msg.ExceptionMessage,
                    Stacktrace = msg.Stacktrace,
                    CreatedAt = msg.CreatedAt
                }).ToList();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Could not delete log messages", nameof(LogService), exception, LogMessageTypeEnum.Error);

                throw;
            }
        }
    }
}
