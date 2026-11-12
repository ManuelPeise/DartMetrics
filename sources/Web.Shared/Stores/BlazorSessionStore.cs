using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.DataProtection;
using Shared.Interfaces.Blazor.BlazorSession;
using Shared.Models.Blazor;
using System.Security.Cryptography;
using System.Text.Json;

namespace Web.Shared.Stores
{
    public sealed class BlazorSessionStore : ISessionStore
    {
        private readonly IDistributedCache _cache;

        private readonly IDataProtector _protector;

        public BlazorSessionStore(IDistributedCache cache, IDataProtectionProvider protectionProvider)
        {
            _cache = cache;
            _protector = protectionProvider.CreateProtector(
                "DartMetrics.Session.v1");
        }

        public async Task<string> CreateAsync(
            BlazorSession session,
            CancellationToken cancellationToken = default)
        {
            var sessionId = Convert.ToHexString(
                RandomNumberGenerator.GetBytes(32));

            var key = KeyPrefix + sessionId;

            var json = JsonSerializer.Serialize(session);
            var protectedJson = _protector.Protect(json);

            await _cache.SetStringAsync(
                key,
                protectedJson,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = SessionLifetime
                },
                cancellationToken);

            return sessionId;
        }

        public async Task<BlazorSession?> GetAsync(
            string sessionId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return null;

            var key = KeyPrefix + sessionId;

            var protectedJson = await _cache.GetStringAsync(
                key,
                cancellationToken);

            if (string.IsNullOrEmpty(protectedJson))
                return null;

            try
            {
                var json = _protector.Unprotect(protectedJson);

                return JsonSerializer.Deserialize<BlazorSession>(json);
            }
            catch (CryptographicException)
            {
                await _cache.RemoveAsync(key, cancellationToken);
                return null;
            }
            catch (JsonException)
            {
                await _cache.RemoveAsync(key, cancellationToken);
                return null;
            }
        }

        public Task DeleteAsync(
            string sessionId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return Task.CompletedTask;

            return _cache.RemoveAsync(
                KeyPrefix + sessionId,
                cancellationToken);
        }

        public async Task UpdateAsync(
            string sessionId,
            BlazorSession session,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            ArgumentNullException.ThrowIfNull(session);

            var key = KeyPrefix + sessionId;

            var json = JsonSerializer.Serialize(session);
            var protectedJson = _protector.Protect(json);

            await _cache.SetStringAsync(
                key,
                protectedJson,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = SessionLifetime
                },
                cancellationToken);
        }

        private const string KeyPrefix = "dartmetrics:blazor:session:";

        private static readonly TimeSpan SessionLifetime =
            TimeSpan.FromHours(8);
    }
}
