using Shared.Enums.Logger;

namespace Shared.Models.Administration.Log
{
    public class LogmessageExportModel
    {
        public int Id { get; set; }
        public string Module { get; set; } = null!;
        public string Message { get; set; } = null!;
        public LogMessageTypeEnum MessageType { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? Stacktrace { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
