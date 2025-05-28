using Carter;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Extension
{
    public static class CarterExtension
    {
        public static void AddCarterExtension(this IServiceCollection services)
        {
            //services.AddCarter();
            services.AddCarter(configurator: c =>
            {
                c.WithValidatorLifetime(ServiceLifetime.Scoped);
            });
        }
    }
}
