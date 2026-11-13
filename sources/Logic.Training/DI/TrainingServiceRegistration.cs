using Microsoft.Extensions.DependencyInjection;
using Shared.Interfaces.Training;

namespace Logic.Training.DI
{
    public static class TrainingServiceRegistration
    {
        public static void AddTrainingServices(this IServiceCollection services)
        {
            services.AddScoped<ISegmentTraining, SegmentTraining>();
            services.AddScoped<ICheckoutTraining, CheckoutTraining>();
        }
    }
}
