namespace Shared.Models.Options
{
    public class EmailOptions
    {
        public string SmtpServer { get; set; } = null!;
        public int Port { get; set; }
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FromAddress { get; set; } = null!;
        public bool EnableSsl { get; set; }
    }
}
