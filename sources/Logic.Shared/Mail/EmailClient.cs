using Microsoft.Extensions.Options;
using Shared.Interfaces.Email;
using Shared.Interfaces.Logger;
using Shared.Models.Email;
using Shared.Models.Options;
using System.Net.Mail;

namespace Logic.Shared.Mail
{
    public class EmailClient : IEmailClient
    {
        private readonly EmailOptions _emailOptions;
        private readonly IMessageLog _messageLog;

        public EmailClient(IMessageLog messageLog, IOptions<EmailOptions> emailOptions)
        {
            _messageLog = messageLog;
            _emailOptions = emailOptions?.Value ?? throw new ArgumentNullException(nameof(emailOptions));
        }

        public async Task SendMail(EmailSettings emailSettings)
        {
            try
            {
                ArgumentException.ThrowIfNullOrEmpty(emailSettings.ToAddress, nameof(emailSettings.ToAddress));
                ArgumentException.ThrowIfNullOrEmpty(emailSettings.Subject, nameof(emailSettings.Subject));
                ArgumentException.ThrowIfNullOrEmpty(emailSettings.Body, nameof(emailSettings.Body));

                using (var smtpClient = new SmtpClient(_emailOptions.SmtpServer, _emailOptions.Port))
                {
                    smtpClient.Credentials = new System.Net.NetworkCredential(_emailOptions.Username, _emailOptions.Password);
                    smtpClient.EnableSsl = _emailOptions.EnableSsl;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_emailOptions.FromAddress),
                        Subject = emailSettings.Subject,
                        Body = emailSettings.Body,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(emailSettings.ToAddress);

                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (Exception exception)
            {
                await _messageLog.LogMessage("Unable to send email", nameof(EmailClient), exception);
                throw;
            }
        }
    }
}
