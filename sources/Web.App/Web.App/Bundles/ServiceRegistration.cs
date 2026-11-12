using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Shared.Interfaces.Blazor.Authentication;
using Shared.Models.Options.Blazor;
using Web.Shared.Constants;
using Web.Shared.DI;

namespace Web.App.Bundles
{
    public static class ServiceRegistration
    {
        private const string ApplicationName = "DartMetrics";

        public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<ApiHttpClientOptions>()
                .Bind(configuration.GetSection(nameof(ApiHttpClientOptions)))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // TODO production: persist the keys (PersistKeysToFileSystem / Redis), otherwise every restart logs out all users.
            services.AddDataProtection()
                .SetApplicationName(ApplicationName);

            AddCookieAuthentication(services);

            // Session store, token refresh, API clients (Web.Shared)
            services.RegisterWebServices();

            // HttpClient for the BFF proxy (/bff/api/** -> DartMetrics API)
            services.AddHttpClient(BffEndpoints.ProxyClientName, (serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<ApiHttpClientOptions>>().Value;
                client.BaseAddress = new Uri($"{options.ApiBaseUrl}:{options.Port}/api/");
            });

            services.AddCascadingAuthenticationState();

            services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();
        }

        private static void AddCookieAuthentication(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = WebServiceRegistration.Scheme;
                options.DefaultChallengeScheme = WebServiceRegistration.Scheme;
                options.DefaultSignInScheme = WebServiceRegistration.Scheme;
            })
            .AddCookie(WebServiceRegistration.Scheme, options =>
            {
                options.Cookie.Name = "__Host-DartMetrics.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = "/";

                options.LoginPath = "/login";
                options.AccessDeniedPath = "/access-denied";
                options.ReturnUrlParameter = "returnUrl";

                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = false;

                options.Events.OnValidatePrincipal = async context =>
                {
                    var sessionId = context.Principal?.FindFirst(BlazorClaimTypes.SessionId)?.Value;

                    var isValid = !string.IsNullOrWhiteSpace(sessionId) &&
                        await context.HttpContext.RequestServices
                            .GetRequiredService<ITokenRefreshService>()
                            .RefreshIfNeededAsync(sessionId, context.HttpContext.RequestAborted);

                    if (!isValid)
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(WebServiceRegistration.Scheme);
                    }
                };

                // API-style requests (/bff/...) get status codes, page requests get redirected.
                options.Events.OnRedirectToLogin = context =>
                {
                    if (IsBffRequest(context.Request))
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    else
                        context.Response.Redirect(context.RedirectUri);

                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (IsBffRequest(context.Request))
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    else
                        context.Response.Redirect(context.RedirectUri);

                    return Task.CompletedTask;
                };
            });

            services.AddAuthorization();
        }

        private static bool IsBffRequest(HttpRequest request) =>
            request.Path.StartsWithSegments("/bff");
    }
}
