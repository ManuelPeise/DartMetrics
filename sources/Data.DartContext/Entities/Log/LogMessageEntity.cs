using Shared.Enums.Logger;

namespace Data.DartContext.Entities.Log
{
    public class LogMessageEntity: AEntityBase
    {
        public string Message { get; set; } = null!;
        public string Module { get; set; } = null!;
        public string? ExceptionMessage { get; set; }
        public string? Stacktrace { get; set; }
        public LogMessageTypeEnum LogMessageType { get; set; }
    }
}
