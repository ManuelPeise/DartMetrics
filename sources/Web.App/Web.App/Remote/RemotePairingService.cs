using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Web.App.Remote
{
    /// <summary>
    /// Kopplung Handy ↔ Browser-Sitzung ohne Datenbank:
    /// 1. Die angemeldete Spielseite holt einen einmaligen Code (kurz gültig, im Cache).
    /// 2. Das Handy löst ihn ein und bekommt einen Geräte-Token (Data Protection, zeitlich begrenzt),
    ///    der an Benutzer und Sitzung gebunden ist. Abmelden im Browser beendet damit auch das Handy.
    /// </summary>
    public sealed class RemotePairingService
    {
        private const string CacheKeyPrefix = "dartmetrics:remote:pair:";
        private const int MaxDeviceNameLength = 60;

        private readonly IDistributedCache _cache;
        private readonly ITimeLimitedDataProtector _protector;
        private readonly RemoteInputOptions _options;

        public RemotePairingService(IDistributedCache cache, IDataProtectionProvider protectionProvider, IOptions<RemoteInputOptions> options)
        {
            _cache = cache;
            _protector = protectionProvider.CreateProtector("DartMetrics.RemoteDevice.v1").ToTimeLimitedDataProtector();
            _options = options.Value;
        }

        public async Task<(string Code, DateTime ExpiresAtUtc)> CreatePairingAsync(
            string userId, string sessionId, string userName, CancellationToken cancellationToken)
        {
            var code = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(24));
            var expiresAt = DateTime.UtcNow.AddSeconds(_options.PairingCodeSeconds);

            var entry = JsonSerializer.Serialize(new PendingPairing(userId, sessionId, userName));

            await _cache.SetStringAsync(CacheKeyPrefix + code, entry, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = expiresAt
            }, cancellationToken);

            return (code, expiresAt);
        }

        /// <summary>Löst einen Code genau einmal ein. Null: unbekannt, abgelaufen oder schon benutzt.</summary>
        public async Task<(string Token, DateTime ExpiresAtUtc, string UserName)?> RedeemAsync(
            string code, string? deviceName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length > 64)
            {
                return null;
            }

            var key = CacheKeyPrefix + code;
            var entry = await _cache.GetStringAsync(key, cancellationToken);

            if (entry is null)
            {
                return null;
            }

            await _cache.RemoveAsync(key, cancellationToken);

            var pending = JsonSerializer.Deserialize<PendingPairing>(entry);

            if (pending is null)
            {
                return null;
            }

            var name = string.IsNullOrWhiteSpace(deviceName) ? "Handy" : deviceName.Trim();
            if (name.Length > MaxDeviceNameLength)
            {
                name = name[..MaxDeviceNameLength];
            }

            var expiresAt = DateTime.UtcNow.AddHours(_options.DeviceTokenHours);
            var payload = new RemoteDeviceTicket(pending.UserId, pending.SessionId, pending.UserName, Guid.NewGuid().ToString("N"), name);
            var token = _protector.Protect(JsonSerializer.Serialize(payload), new DateTimeOffset(expiresAt, TimeSpan.Zero));

            return (token, expiresAt, pending.UserName);
        }

        /// <summary>Prüft Signatur und Ablauf des Geräte-Tokens.</summary>
        public RemoteDeviceTicket? ReadToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > 4096)
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<RemoteDeviceTicket>(_protector.Unprotect(token));
            }
            catch (CryptographicException)
            {
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private sealed record PendingPairing(string UserId, string SessionId, string UserName);
    }

    public sealed record RemoteDeviceTicket(string UserId, string SessionId, string UserName, string DeviceId, string DeviceName);
}
