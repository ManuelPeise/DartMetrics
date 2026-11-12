using Microsoft.Extensions.DependencyInjection;
using Shared.Interfaces.User;
using System;

namespace Logic.Profile.DI
{
    public static class ProfileServiceRegistration
    {
        public static void RegisterProfileServices(this IServiceCollection services)
        {
            services.AddScoped<IUserProfileService, UserProfileService>();
        }
    }
}
