using Shared.Interfaces.Blazor.Api;
using Shared.Interfaces.Blazor.Authentication;
using Shared.Interfaces.Blazor.BlazorSession;
using Shared.Models.Authentication;
using Shared.Models.User;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Web.App.Client.Authentication;
using Web.App.Client.Services;
using Web.Shared.Constants;

namespace Web.App.Bundles
{
    /// <summary>
    /// Backend-for-Frontend endpoints for the WebAssembly client.
    /// Login/refresh/logout go to the API's UserAuthenticationController; the JWTs stay in the server-side
    /// session store and never reach the browser. The browser only gets the HttpOnly auth cookie.
    /// </summary>
    public static class BffEndpoints
    {
        public const string ProxyClientName = "BffApiProxy";

        // Rate-limit policy for the anonymous auth endpoints (login, register, reset ...), see ServiceRegistration.
        public const string AuthRateLimitPolicy = "bff-auth";

        // API areas that must not be reachable through the generic proxy.
        private static readonly string[] BlockedProxyPrefixes = ["userauthentication"];

        public static void MapBffEndpoints(this WebApplication app)
        {
            var bff = app.MapGroup("/bff")
                .AddEndpointFilter(RequireCsrfHeader)
                .DisableAntiforgery();

            bff.MapGet("/user", GetUser).AllowAnonymous();
            bff.MapPost("/login", LoginAsync).AllowAnonymous().RequireRateLimiting(AuthRateLimitPolicy);
            bff.MapPost("/register", RegisterAsync).AllowAnonymous().RequireRateLimiting(AuthRateLimitPolicy);
            bff.MapPost("/verify-email", VerifyEmailAsync).AllowAnonymous().RequireRateLimiting(AuthRateLimitPolicy);
            bff.MapPost("/resend-verification", ResendVerificationAsync).AllowAnonymous().RequireRateLimiting(AuthRateLimitPolicy);
            bff.MapPost("/forgot-password", ForgotPasswordAsync).AllowAnonymous().RequireRateLimiting(AuthRateLimitPolicy);
            bff.MapPost("/reset-password", ResetPasswordAsync).AllowAnonymous().RequireRateLimiting(AuthRateLimitPolicy);
            bff.MapPost("/logout", LogoutAsync).RequireAuthorization();

            bff.Map("/api/{**path}", ProxyAsync).RequireAuthorization();
        }

        internal static async ValueTask<object?> RequireCsrfHeader(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            if (!context.HttpContext.Request.Headers.ContainsKey(BffHttpClient.CsrfHeaderName))
                return Results.BadRequest("Missing CSRF header.");

            return await next(context);
        }

        private static IResult GetUser(HttpContext context)
        {
            var user = context.User;

            if (user.Identity?.IsAuthenticated != true)
                return Results.Ok(new BffUserInfo { IsAuthenticated = false });

            return Results.Ok(new BffUserInfo
            {
                IsAuthenticated = true,
                UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Name = user.Identity.Name,
                Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
            });
        }

        private static async Task<IResult> LoginAsync(
            LoginModel model,
            HttpContext context,
            IBlazorAuthenticationService authenticationService,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(model.EmailAddress) || string.IsNullOrWhiteSpace(model.Password))
                return Results.BadRequest();

            try
            {
                var success = await authenticationService.LoginAsync(
                    context,
                    new AuthenticationRequest { EmailAddress = model.EmailAddress, Password = model.Password },
                    cancellationToken);

                return success ? Results.Ok() : Results.Unauthorized();
            }
            catch (HttpRequestException)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        private static async Task<IResult> RegisterAsync(
            RegisterModel model,
            IApiHttpClientService apiHttpClientService,
            CancellationToken cancellationToken)
        {
            // Server-side validation as well - the client-side checks can be bypassed.
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(model, new ValidationContext(model), validationResults, validateAllProperties: true))
                return Results.BadRequest();

            var request = new UserRegistrationRequestModel
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(model.DisplayName) ? null : model.DisplayName.Trim(),
                EmailAddress = model.EmailAddress.Trim(),
                UserCredentials = new UserCredentialsModelBase
                {
                    Password = model.Password,
                    PasswordReplication = model.PasswordReplication
                }
            };

            try
            {
                // Anonymous call to RegistrationController/RegisterUser (returns true/false).
                using var response = await apiHttpClientService.PostAsync(
                    "registration/registeruser",
                    JsonContent.Create(request),
                    cancellationToken: cancellationToken);

                var success = response.IsSuccessStatusCode &&
                              await response.Content.ReadFromJsonAsync<bool>(cancellationToken);

                return success ? Results.Ok() : Results.BadRequest();
            }
            catch (HttpRequestException)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        private static async Task<IResult> VerifyEmailAsync(
            VerifyEmailModel model,
            IApiHttpClientService apiHttpClientService,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(model.Token) || model.Token.Length > 4096)
                return Results.BadRequest();

            try
            {
                // RegistrationController/VerifyEmail validates the token (signature, audience, lifetime): 200 or 400.
                using var response = await apiHttpClientService.PostAsync(
                    $"registration/verifyemail?token={Uri.EscapeDataString(model.Token)}",
                    cancellationToken: cancellationToken);

                return response.IsSuccessStatusCode ? Results.Ok() : Results.BadRequest();
            }
            catch (HttpRequestException)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        private static Task<IResult> ResendVerificationAsync(EmailAddressRequestModel model, IApiHttpClientService api, CancellationToken ct) =>
            ForwardEmailAsync("registration/resendverificationmail", model, api, ct);

        private static Task<IResult> ForgotPasswordAsync(EmailAddressRequestModel model, IApiHttpClientService api, CancellationToken ct) =>
            ForwardEmailAsync("registration/requestpasswordreset", model, api, ct);

        /// <summary>Immer 200, solange die Adresse plausibel ist – die API verrät nicht, ob es das Konto gibt.</summary>
        private static async Task<IResult> ForwardEmailAsync(string path, EmailAddressRequestModel model, IApiHttpClientService api, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(model.EmailAddress) || model.EmailAddress.Length > 254 || !new EmailAddressAttribute().IsValid(model.EmailAddress))
                return Results.BadRequest();

            try
            {
                using var response = await api.PostAsync(path, JsonContent.Create(new EmailAddressRequestModel { EmailAddress = model.EmailAddress.Trim() }), cancellationToken: ct);
                return response.IsSuccessStatusCode ? Results.Ok() : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            catch (HttpRequestException)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        private static async Task<IResult> ResetPasswordAsync(ResetPasswordRequestModel model, IApiHttpClientService api, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(model.Token) || model.Token.Length > 4096 ||
                string.IsNullOrEmpty(model.Password) || model.Password != model.PasswordReplication)
                return Results.BadRequest();

            try
            {
                using var response = await api.PostAsync("registration/resetpassword", JsonContent.Create(model), cancellationToken: ct);
                return response.IsSuccessStatusCode ? Results.Ok() : Results.BadRequest();
            }
            catch (HttpRequestException)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        private static async Task<IResult> LogoutAsync(HttpContext context, IBlazorAuthenticationService authenticationService)
        {
            await authenticationService.LogoutAsync(context);
            return Results.Ok();
        }

        private static async Task ProxyAsync(
            HttpContext context,
            string? path,
            IHttpClientFactory httpClientFactory,
            ITokenRefreshService tokenRefreshService,
            ISessionStore sessionStore)
        {
            var cancellationToken = context.RequestAborted;
            var client = httpClientFactory.CreateClient(ProxyClientName);

            // Build the target URI safely: no absolute URLs, no "..", must stay below .../api/ (prevents SSRF / token leaks).
            if (string.IsNullOrWhiteSpace(path) ||
                path.Contains("..", StringComparison.Ordinal) ||
                path.Contains("://", StringComparison.Ordinal) ||
                BlockedProxyPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) ||
                !Uri.TryCreate(client.BaseAddress!, path.TrimStart('/') + context.Request.QueryString, out var targetUri) ||
                !client.BaseAddress!.IsBaseOf(targetUri))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var sessionId = context.User.FindFirst(BlazorClaimTypes.SessionId)?.Value;

            if (string.IsNullOrWhiteSpace(sessionId) ||
                !await tokenRefreshService.RefreshIfNeededAsync(sessionId, cancellationToken))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            // Buffer the body so the request can be retried after a forced token refresh.
            byte[]? body = null;

            if (context.Request.ContentLength > 0 || context.Request.Headers.TransferEncoding.Count > 0)
            {
                using var buffer = new MemoryStream();
                await context.Request.Body.CopyToAsync(buffer, cancellationToken);
                body = buffer.ToArray();
            }

            var response = await SendAsync(client, context.Request, targetUri, body, sessionId, sessionStore, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Dispose();

                if (!await tokenRefreshService.ForceRefreshAsync(sessionId, cancellationToken))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                response = await SendAsync(client, context.Request, targetUri, body, sessionId, sessionStore, cancellationToken);
            }

            using (response)
            {
                context.Response.StatusCode = (int)response.StatusCode;

                if (response.Content.Headers.ContentType != null)
                    context.Response.ContentType = response.Content.Headers.ContentType.ToString();

                // Für Bilder (Profilbild): Cache- und nosniff-Header der API durchreichen.
                if (response.Headers.CacheControl != null)
                    context.Response.Headers.CacheControl = response.Headers.CacheControl.ToString();

                if (response.Headers.TryGetValues("X-Content-Type-Options", out var noSniff))
                    context.Response.Headers.XContentTypeOptions = noSniff.ToArray();

                await response.Content.CopyToAsync(context.Response.Body, cancellationToken);
            }
        }

        private static async Task<HttpResponseMessage> SendAsync(
            HttpClient client,
            HttpRequest request,
            Uri targetUri,
            byte[]? body,
            string sessionId,
            ISessionStore sessionStore,
            CancellationToken cancellationToken)
        {
            var session = await sessionStore.GetAsync(sessionId, cancellationToken);

            using var message = new HttpRequestMessage(new HttpMethod(request.Method), targetUri);

            if (body != null)
            {
                message.Content = new ByteArrayContent(body);

                if (!string.IsNullOrEmpty(request.ContentType))
                    message.Content.Headers.TryAddWithoutValidation("Content-Type", request.ContentType);
            }

            if (!string.IsNullOrEmpty(request.Headers.Accept))
                message.Headers.TryAddWithoutValidation("Accept", request.Headers.Accept.ToString());

            if (!string.IsNullOrWhiteSpace(session?.AccessToken))
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

            return await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
    }
}
