using Shared.Interfaces.Blazor.Api;
using Shared.Interfaces.Blazor.Authentication;
using Shared.Interfaces.Blazor.BlazorSession;
using Shared.Models.Authentication;
using Shared.Models.Blazor;
using System.Collections.Concurrent;

namespace Web.Shared.Services
{
    public class TokenRefreshService : ITokenRefreshService
    {
        private readonly IBlazorAuthenticationService _blazorAuthenticationService;
        private readonly ISessionStore _sessionStore;

        // Refresh tokens are rotated on every use. Parallel requests of the same session must not refresh
        // simultaneously, otherwise the second request uses an already rotated token and the user is logged out.
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> SessionLocks = new();

        public TokenRefreshService(
            IBlazorAuthenticationService blazorAuthenticationService,
            ISessionStore sessionStore)
        {
            _blazorAuthenticationService = blazorAuthenticationService;
            _sessionStore = sessionStore;
        }

        public async Task<bool> RefreshIfNeededAsync(
            string sessionId,
            CancellationToken cancellationToken = default)
        {
            var session = await _sessionStore.GetAsync(
                sessionId,
                cancellationToken);

            if (session == null)
                return false;

            if (!NeedsRefresh(session))
                return session.RefreshTokenExpiresAt > DateTimeOffset.UtcNow;

            return await RunLockedAsync(sessionId, force: false, rejectedAccessToken: null, cancellationToken);
        }

        public async Task<bool> ForceRefreshAsync(
            string sessionId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return false;

            // Remember which token was rejected. If another request already refreshed while we were
            // waiting for the lock, the new token is used instead of rotating again.
            var rejectedAccessToken = (await _sessionStore.GetAsync(sessionId, cancellationToken))?.AccessToken;

            return await RunLockedAsync(sessionId, force: true, rejectedAccessToken, cancellationToken);
        }

        private async Task<bool> RunLockedAsync(
            string sessionId,
            bool force,
            string? rejectedAccessToken,
            CancellationToken cancellationToken)
        {
            var sessionLock = SessionLocks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));

            await sessionLock.WaitAsync(cancellationToken);

            try
            {
                return await RefreshLockedAsync(sessionId, force, rejectedAccessToken, cancellationToken);
            }
            finally
            {
                sessionLock.Release();
            }
        }

        private static bool NeedsRefresh(BlazorSession session) =>
            !string.IsNullOrEmpty(session.RefreshToken) &&
            session.AccessTokenExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(2);

        private async Task<bool> RefreshLockedAsync(
            string sessionId,
            bool force,
            string? rejectedAccessToken,
            CancellationToken cancellationToken)
        {
            // Re-read inside the lock: another request may have refreshed the session in the meantime.
            var session = await _sessionStore.GetAsync(
                sessionId,
                cancellationToken);

            if (session == null)
            {
                SessionLocks.TryRemove(sessionId, out _);
                return false;
            }

            if (session.RefreshTokenExpiresAt <= DateTimeOffset.UtcNow)
            {
                await _sessionStore.DeleteAsync(
                    sessionId,
                    cancellationToken);

                SessionLocks.TryRemove(sessionId, out _);
                return false;
            }

            if (string.IsNullOrEmpty(session.RefreshToken))
            {
                return !force;
            }

            if (!force && !NeedsRefresh(session))
            {
                return true;
            }

            if (force && rejectedAccessToken != null && session.AccessToken != rejectedAccessToken)
            {
                return true;
            }

            var tokenResponse = await _blazorAuthenticationService.RefreshAsync(session.RefreshToken, session.AccessToken, cancellationToken);

            if (tokenResponse == null ||
                string.IsNullOrWhiteSpace(tokenResponse.Token) ||
                string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
            {
                await _sessionStore.DeleteAsync(
                    sessionId,
                    cancellationToken);

                SessionLocks.TryRemove(sessionId, out _);
                return false;
            }

            var updatedSession = new BlazorSession
            {
                AccessToken = tokenResponse.Token,
                RefreshToken = tokenResponse.RefreshToken,
                AccessTokenExpiresAt = tokenResponse.AccessTokenExpiresAt,
                RefreshTokenExpiresAt = tokenResponse.RefreshTokenExpiresAt,
                UserId = session.UserId,
                Email = session.Email,
                Roles = session.Roles
            };

            await _sessionStore.UpdateAsync(
                sessionId,
                updatedSession,
                cancellationToken);

            return true;
        }
    }
}
