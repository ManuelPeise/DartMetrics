using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Interfaces.Blazor.Api;
using Shared.Interfaces.Blazor.Authentication;
using Shared.Interfaces.Blazor.BlazorSession;
using Shared.Models.Options.Blazor;
using Web.Shared.Constants;
using Web.Shared.Services;
using Web.Shared.Stores;

namespace Web.Shared.DI
{
    public static class WebServiceRegistration
    {
        public const string Scheme = "DartMetrics.Cookie";

        public static void RegisterWebServices(this IServiceCollection services)
        {

            services.AddScoped<IBlazorAuthenticationService, BlazorAuthenticationService>();

            services.AddHttpClient<IApiHttpClientService, ApiHttpClientService>((serviceProvider, client) =>
            {
                var options = serviceProvider
                .GetRequiredService<IOptions<ApiHttpClientOptions>>()
                .Value;

                client.BaseAddress = new Uri($"{options.ApiBaseUrl}:{options.Port}/api/");
            });

            services.AddDistributedMemoryCache();

            services.AddScoped<ISessionStore, BlazorSessionStore>();
            services.AddScoped<ITokenRefreshService, TokenRefreshService>();

            // Use this client in components/services for all calls on behalf of the signed-in user.
            services.AddScoped<IAuthorizedApiClient, AuthorizedApiClient>();
        }
    }
}
