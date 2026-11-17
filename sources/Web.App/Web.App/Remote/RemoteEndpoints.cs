using System.Security.Claims;
using Microsoft.Extensions.Options;
using QRCoder;
using Shared.Models.Remote;
using Web.App.Bundles;
using Web.Shared.Constants;

namespace Web.App.Remote
{
    /// <summary>
    /// Endpunkte für das Handy als Eingabegerät:
    /// POST /bff/remote/pairing (Browser, Cookie + CSRF) -> Kopplungscode und QR-Code,
    /// POST /remote/pair (Handy, anonym, Rate Limit) -> Geräte-Token,
    /// Hub /hubs/remote (Browser per Cookie, Handy per Geräte-Token).
    /// </summary>
    public static class RemoteEndpoints
    {
        public static void MapRemoteEndpoints(this WebApplication app)
        {
            app.MapGroup("/bff/remote")
                .AddEndpointFilter(BffEndpoints.RequireCsrfHeader)
                .DisableAntiforgery()
                .MapPost("/pairing", CreatePairingAsync)
                .RequireAuthorization();

            app.MapPost("/remote/pair", PairAsync)
                .AllowAnonymous()
                .DisableAntiforgery()
                .RequireRateLimiting(BffEndpoints.AuthRateLimitPolicy);

            app.MapHub<RemoteInputHub>(RemoteHubMethods.HubPath);
        }

        private static async Task<IResult> CreatePairingAsync(
            HttpContext context,
            RemotePairingService pairing,
            IOptions<RemoteInputOptions> options,
            CancellationToken cancellationToken)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionId = context.User.FindFirstValue(BlazorClaimTypes.SessionId);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(sessionId))
            {
                return Results.Unauthorized();
            }

            var (code, expiresAt) = await pairing.CreatePairingAsync(userId, sessionId, context.User.Identity?.Name ?? "", cancellationToken);

            var server = string.IsNullOrWhiteSpace(options.Value.PublicBaseUrl)
                ? $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}"
                : options.Value.PublicBaseUrl.TrimEnd('/');

            // Normale Adresse: Die Kamera-App des Handys öffnet direkt die Seite /remote (keine App nötig).
            var payload = $"{server}/remote?code={Uri.EscapeDataString(code)}";

            return Results.Ok(new RemotePairingInfo
            {
                Code = code,
                QrPayload = payload,
                QrSvg = RenderQrSvg(payload),
                ExpiresAtUtc = expiresAt
            });
        }

        private static async Task<IResult> PairAsync(
            RemotePairRequest request,
            RemotePairingService pairing,
            CancellationToken cancellationToken)
        {
            var result = await pairing.RedeemAsync(request.Code, request.DeviceName, cancellationToken);

            // Keine Unterscheidung zwischen unbekannt, abgelaufen und schon benutzt.
            if (result is not { } paired)
            {
                return Results.BadRequest("Pairing code is invalid or expired.");
            }

            return Results.Ok(new RemotePairResponse
            {
                DeviceToken = paired.Token,
                ExpiresAtUtc = paired.ExpiresAtUtc,
                HubPath = RemoteHubMethods.HubPath,
                UserName = paired.UserName
            });
        }

        /// <summary>Schwarz auf Weiß mit Ruhezone, damit jede Kamera ihn liest (auch auf dunklem Hintergrund der Seite).</summary>
        private static string RenderQrSvg(string payload)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            return new SvgQRCode(data).GetGraphic(8, "#000000", "#ffffff", true, SvgQRCode.SizingMode.ViewBoxAttribute);
        }
    }
}
