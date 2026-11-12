namespace Shared.Models.Email
{
    public class EmailSettings
    {
        public string ToAddress { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string Body { get; set; } = null!;
    }
}
