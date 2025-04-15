using Application.Common.Behaviours;
using Application.Exceptions;
using Application.Features.Authentication.Commands;
using FluentValidation;
using Infrastructure.DependencyInjection;
using MediatR;
using System.Reflection;

namespace Application.Utilities
{
    public class UserServiceManager : IServicesRegistration
    {
        public void AddServices(IServiceCollection services)
        {
            // ✅ Register all validators from Application layer
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), ServiceLifetime.Scoped);

            // ✅ Register MediatR handlers from Application layer
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
           


            // ✅ Add pipeline behavior (Validation)
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            // ✅ Global exception handling filter
            services.AddControllersWithViews(options =>
            {
                options.Filters.Add<ApiExceptionFilter>();
            });

            services.AddScoped<ApiExceptionFilter>();
        }
    }
}
