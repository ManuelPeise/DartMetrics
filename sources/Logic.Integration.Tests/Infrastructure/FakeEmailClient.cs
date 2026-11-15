using System.Collections.Concurrent;
using Shared.Interfaces.Email;
using Shared.Models.Email;

namespace Logic.Integration.Tests.Infrastructure
{
    /// <summary>Sammelt gesendete Mails statt sie zu verschicken (Reset-Links, Bestätigungen).</summary>
    public sealed class FakeEmailClient : IEmailClient
    {
        public ConcurrentQueue<EmailSettings> Sent { get; } = new();

        public Task SendMail(EmailSettings emailSettings)
        {
            Sent.Enqueue(emailSettings);
            return Task.CompletedTask;
        }
    }
}
