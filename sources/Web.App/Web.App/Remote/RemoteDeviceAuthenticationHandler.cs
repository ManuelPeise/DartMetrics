using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Shared.Interfaces.Blazor.BlazorSession;
using Shared.Models.Remote;
using Web.Shared.Constants;

namespace Web.App.Remote
{
    /// <summary>
    /// Anmeldung des Handys am Hub mit dem Geräte-Token: Header "Authorization: Bearer …" oder – für WebSockets –
    /// Query "access_token" (nur auf /hubs/remote). Gültig, solange die Browser-Sitzung des Benutzers besteht.
    /// </summary>
    public sealed class RemoteDeviceAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "RemoteDevice";

        private readonly RemotePairingService _pairing;
        private readonly ISessionStore _sessionStore;

        public RemoteDeviceAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            RemotePairingService pairing,
            ISessionStore sessionStore)
            : base(options, logger, encoder)
        {
            _pairing = pairing;
            _sessionStore = sessionStore;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var token = ReadToken();

            if (token is null)
            {
                return AuthenticateResult.NoResult();
            }

            var ticket = _pairing.ReadToken(token);

            if (ticket is null)
            {
                return AuthenticateResult.Fail("Invalid device token.");
            }

            // Abgemeldet oder Sitzung abgelaufen: das Handy verliert den Zugang mit.
            if (await _sessionStore.GetAsync(ticket.SessionId, Context.RequestAborted) is null)
            {
                return AuthenticateResult.Fail("Session ended.");
            }

            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, ticket.UserId),
                new Claim(ClaimTypes.Name, ticket.UserName),
                new Claim(BlazorClaimTypes.SessionId, ticket.SessionId),
                new Claim(RemoteClaimTypes.DeviceId, ticket.DeviceId),
                new Claim(RemoteClaimTypes.DeviceName, ticket.DeviceName)
            ], SchemeName);

            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
        }

        private string? ReadToken()
        {
            var header = Request.Headers.Authorization.ToString();

            if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return header["Bearer ".Length..].Trim();
            }

            if (Request.Path.StartsWithSegments(RemoteHubMethods.HubPath))
            {
                var query = Request.Query["access_token"].ToString();
                return string.IsNullOrEmpty(query) ? null : query;
            }

            return null;
        }
    }
}
