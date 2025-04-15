using Microsoft.Extensions.DependencyInjection;

namespace Application.Extension
{
    public static class CorsExtension
    {
        public static void AddCorsExtension(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });
        }
    }
}
