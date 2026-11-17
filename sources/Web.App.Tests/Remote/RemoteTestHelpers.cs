using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shared.Interfaces.Blazor.BlazorSession;
using Shared.Models.Blazor;
using Web.App.Remote;

namespace Web.App.Tests.Remote
{
    internal static class RemoteTestHelpers
    {
        public static IDistributedCache NewCache() =>
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        /// <summary>Gemeinsamer Schlüsselring, damit Token zwischen zwei Service-Instanzen lesbar sind.</summary>
        public static IDataProtectionProvider NewProtection() => new EphemeralDataProtectionProvider();

        public static RemotePairingService NewPairing(
            IDistributedCache? cache = null,
            IDataProtectionProvider? protection = null,
            int pairingCodeSeconds = 120,
            int deviceTokenHours = 12) =>
            new(cache ?? NewCache(), protection ?? NewProtection(), Options.Create(new RemoteInputOptions
            {
                PairingCodeSeconds = pairingCodeSeconds,
                DeviceTokenHours = deviceTokenHours
            }));
    }

    /// <summary>Sitzungsspeicher im Speicher: nur „existiert die Sitzung?“ ist für die Tests wichtig.</summary>
    internal sealed class FakeSessionStore : ISessionStore
    {
        private readonly HashSet<string> _sessions = [];

        public void Add(string sessionId) => _sessions.Add(sessionId);

        public Task<string> CreateAsync(BlazorSession session, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BlazorSession?> GetAsync(string sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_sessions.Contains(sessionId) ? new BlazorSession { AccessToken = "test" } : null);

        public Task UpdateAsync(string sessionId, BlazorSession session, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            _sessions.Remove(sessionId);
            return Task.CompletedTask;
        }
    }
}
