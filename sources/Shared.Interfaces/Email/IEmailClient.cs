using Shared.Models.Email;

namespace Shared.Interfaces.Email
{
    public interface IEmailClient
    {
        Task SendMail(EmailSettings emailSettings);
    }
}
