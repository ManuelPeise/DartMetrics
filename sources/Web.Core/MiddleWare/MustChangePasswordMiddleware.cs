using Microsoft.AspNetCore.Mvc;

namespace Web.Core.MiddleWare
{
    /// <summary>
    /// Einmal-Passwort (Claim "MustChangePassword" im Access-Token): bis zur Änderung sind nur Profil laden,
    /// Passwort ändern und An-/Abmelden erlaubt; alles andere bekommt 403. Nach der Änderung meldet sich der Client neu an.
    /// </summary>
    public class MustChangePasswordMiddleware
    {
        public const string ClaimType = "MustChangePassword";

        private static readonly string[] AllowedPrefixes =
        [
            "/api/Profile/ChangePassword",
            "/api/Profile/GetProfile",
            "/api/UserAuthentication/"
        ];

        private readonly RequestDelegate _next;

        public MustChangePasswordMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var mustChange = context.User.Identity?.IsAuthenticated == true &&
                             string.Equals(context.User.FindFirst(ClaimType)?.Value, "true", StringComparison.OrdinalIgnoreCase);

            if (mustChange && !AllowedPrefixes.Any(p => context.Request.Path.StartsWithSegments(p.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = "Password change required. Please change your password first.",
                    Instance = context.Request.Path
                });
                return;
            }

            await _next(context);
        }
    }
}
