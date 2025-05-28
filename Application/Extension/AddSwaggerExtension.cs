/*using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Application.Extension
{
    public static class SwaggerExtension
    {
        public static void AddSwaggerExtension(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "EcommerceInstant API",
                    Version = "v1",
                    Description = "This API is responsible for overall data distribution and authorization."
                });
            });
        }

        public static void UseSwaggerExtension(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "EcommerceInstant API v1");
                c.RoutePrefix = string.Empty; // Serve at root
            });
        }
    }
}
*/