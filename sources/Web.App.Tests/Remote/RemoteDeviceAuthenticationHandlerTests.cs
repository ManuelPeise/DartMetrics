using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Web.App.Remote;

namespace Web.App.Tests.Remote
{
    /// <summary>Anmeldung des Handys: gültiger Token und bestehende Browser-Sitzung, Query-Token nur am Hub.</summary>
    public class RemoteDeviceAuthenticationHandlerTests
    {
        private readonly RemotePairingService _pairing = RemoteTestHelpers.NewPairing();
        private readonly FakeSessionStore _sessions = new();

        [Fact]
        public async Task BearerToken_WithActiveSession_Succeeds()
        {
            _sessions.Add("session-1");
            var token = await PairAsync("session-1");

            var result = await AuthenticateAsync("/remote/anything", bearer: token);

            Assert.True(result.Succeeded);
            Assert.Equal("7", result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier));
            Assert.NotNull(result.Principal.FindFirstValue(RemoteClaimTypes.DeviceId));
        }

        [Fact]
        public async Task SessionEnded_Fails()
        {
            _sessions.Add("session-1");
            var token = await PairAsync("session-1");
            await _sessions.DeleteAsync("session-1");

            var result = await AuthenticateAsync("/hubs/remote", bearer: token);

            Assert.False(result.Succeeded);
            Assert.NotNull(result.Failure);
        }

        [Fact]
        public async Task QueryToken_OnHubPath_Succeeds()
        {
            _sessions.Add("session-1");
            var token = await PairAsync("session-1");

            var result = await AuthenticateAsync("/hubs/remote", query: token);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task QueryToken_OutsideHubPath_IsIgnored()
        {
            _sessions.Add("session-1");
            var token = await PairAsync("session-1");

            var result = await AuthenticateAsync("/bff/api/profile/getprofile", query: token);

            Assert.True(result.None);
        }

        [Fact]
        public async Task NoToken_ReturnsNoResult()
        {
            var result = await AuthenticateAsync("/hubs/remote");

            Assert.True(result.None);
        }

        [Fact]
        public async Task InvalidToken_Fails()
        {
            var result = await AuthenticateAsync("/hubs/remote", bearer: "garbage");

            Assert.False(result.Succeeded);
            Assert.NotNull(result.Failure);
        }

        private async Task<string> PairAsync(string sessionId)
        {
            var (code, _) = await _pairing.CreatePairingAsync("7", sessionId, "max@example.org", default);
            return (await _pairing.RedeemAsync(code, "Pixel", default))!.Value.Token;
        }

        private async Task<AuthenticateResult> AuthenticateAsync(string path, string? bearer = null, string? query = null)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;

            if (bearer is not null)
            {
                context.Request.Headers.Authorization = $"Bearer {bearer}";
            }

            if (query is not null)
            {
                context.Request.QueryString = QueryString.Create("access_token", query);
            }

            var options = new StaticOptionsMonitor(new AuthenticationSchemeOptions());
            var handler = new RemoteDeviceAuthenticationHandler(options, NullLoggerFactory.Instance, UrlEncoder.Default, _pairing, _sessions);

            await handler.InitializeAsync(
                new AuthenticationScheme(RemoteDeviceAuthenticationHandler.SchemeName, null, typeof(RemoteDeviceAuthenticationHandler)),
                context);

            return await handler.AuthenticateAsync();
        }

        private sealed class StaticOptionsMonitor(AuthenticationSchemeOptions value) : IOptionsMonitor<AuthenticationSchemeOptions>
        {
            public AuthenticationSchemeOptions CurrentValue => value;

            public AuthenticationSchemeOptions Get(string? name) => value;

            public IDisposable? OnChange(Action<AuthenticationSchemeOptions, string?> listener) => null;
        }
    }
}
