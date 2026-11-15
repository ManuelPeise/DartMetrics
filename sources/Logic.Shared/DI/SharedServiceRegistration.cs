using Logic.Shared.Users;
using Shared.Interfaces.User;
using Logic.Shared.Helpers;
using Logic.Shared.Logger;
using Logic.Shared.Mail;
using Logic.Shared.Repositories;
using Logic.Shared.UnitsOfWork;
using Microsoft.Extensions.DependencyInjection;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Email;
using Shared.Interfaces.Logger;
using Shared.Interfaces.Repositories;
using Shared.Interfaces.UnitsOfWork;

namespace Logic.Shared.DI
{
    public static class SharedServiceRegistration
    {
        public static void AddSharedServices(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepositoryBase<>), typeof(RepositoryBase<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IMessageLog, MessageLog>();
            services.AddScoped<IClaimsHelper, ClaimsHelper>();
            services.AddScoped<IUserAnonymizer, UserAnonymizer>();
            services.AddScoped<IEmailClient, EmailClient>();
        }
    }
}
