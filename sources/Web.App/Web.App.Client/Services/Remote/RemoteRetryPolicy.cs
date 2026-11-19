using Microsoft.AspNetCore.SignalR.Client;

namespace Web.App.Client.Services.Remote
{
    /// <summary>
    /// Wiederverbinden für Handy und Spielseite: sofort, dann schnell hintereinander, danach alle 3 s –
    /// statt nach vier Versuchen (Standard: 0, 2, 10, 30 s) aufzugeben. Erst nach 10 Minuten ohne Erfolg
    /// gilt die Verbindung als verloren (z. B. Token abgelaufen, Abmeldung im Browser).
    /// </summary>
    public sealed class RemoteRetryPolicy : IRetryPolicy
    {
        private static readonly TimeSpan[] FirstDelays =
        [
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2)
        ];

        private static readonly TimeSpan Delay = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan GiveUpAfter = TimeSpan.FromMinutes(10);

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            if (retryContext.ElapsedTime > GiveUpAfter)
            {
                return null;
            }

            return retryContext.PreviousRetryCount < FirstDelays.Length
                ? FirstDelays[retryContext.PreviousRetryCount]
                : Delay;
        }
    }

    public static class RemoteHubTimeouts
    {
        /// <summary>Ping-Abstand: kurze Abbrüche (WLAN-Stromsparen, Bildschirm aus) werden schnell bemerkt.</summary>
        public static readonly TimeSpan KeepAlive = TimeSpan.FromSeconds(5);

        /// <summary>Ohne Nachricht so lange: Verbindung gilt als weg, Wiederverbinden beginnt.</summary>
        public static readonly TimeSpan ServerTimeout = TimeSpan.FromSeconds(15);
    }
}
