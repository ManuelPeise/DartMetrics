using Shared.Enums.Logger;

namespace Shared.Models.Logger
{
    public class LogMessage
    {
        public int Id { get; set; }
        public string Message { get; set; } = null!;
        public string Module { get; set; } = null!;
        public string? ExceptionMessage { get; set; }
        public string? Stacktrace { get; set; }
        public LogMessageTypeEnum LogMessageType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
