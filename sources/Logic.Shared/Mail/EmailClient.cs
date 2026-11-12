using Microsoft.Extensions.Options;
using Shared.Interfaces.Email;
using Shared.Interfaces.Logger;
using Shared.Models.Email;
using Shared.Models.Options;
using System.Net;
using System.Net.Mail;

namespace Logic.Shared.Mail
{
    public class EmailClient : IEmailClient
    {
        private const int TimeoutMilliseconds = 15_000;

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
                ArgumentException.ThrowIfNullOrEmpty(_emailOptions.SmtpServer, nameof(_emailOptions.SmtpServer));
                ArgumentException.ThrowIfNullOrEmpty(_emailOptions.FromAddress, nameof(_emailOptions.FromAddress));

                using var smtpClient = new SmtpClient(_emailOptions.SmtpServer, _emailOptions.Port)
                {
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    EnableSsl = _emailOptions.EnableSsl,
                    Timeout = TimeoutMilliseconds,
                    UseDefaultCredentials = false
                };

                // Only authenticate when credentials are configured. Mailpit (development) needs none -
                // sending empty credentials makes the SMTP AUTH fail if the server offers authentication.
                if (!string.IsNullOrWhiteSpace(_emailOptions.Username))
                {
                    smtpClient.Credentials = new NetworkCredential(_emailOptions.Username, _emailOptions.Password);
                }

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailOptions.FromAddress, "DartMetrics"),
                    Subject = emailSettings.Subject,
                    Body = emailSettings.Body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(emailSettings.ToAddress);

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception exception)
            {
                // Server/port in the message makes configuration problems obvious in the log.
                await _messageLog.LogMessage(
                    $"Unable to send email via {_emailOptions.SmtpServer}:{_emailOptions.Port} (SSL: {_emailOptions.EnableSsl})",
                    nameof(EmailClient),
                    exception);

                throw;
            }
        }
    }
}
