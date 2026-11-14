using Microsoft.Extensions.DependencyInjection;
using Shared.Interfaces.Authentication;

namespace Logic.Authentication.DI
{
    public static class AuthenticationServices
    {
        public static void AddAuthenticationServices(this IServiceCollection services)
        {
            services.AddScoped<IUserAuthentication, UserAuthentication>();
            services.AddScoped<IUserRegistration, UserRegistration>();
        }
    }
}
