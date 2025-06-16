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

                // ✅ Specific policy for payment callbacks
                options.AddPolicy("PaymentCallbackPolicy", builder =>
                {
                    builder.WithOrigins(
                        "https://rc-epay.esewa.com.np",
                        "https://uat.esewa.com.np",
                        "https://esewa.com.np",
                        "https://khalti.com",
                        "https://test-pay.khalti.com",
                        "https://pay.khalti.com",
                        "http://localhost:5173",
                        "http://localhost:3000",
                        "http://localhost:5225"
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
                });

                // ✅ Most permissive policy for development
                options.AddPolicy("Development", builder =>
                {
                    builder.SetIsOriginAllowed(_ => true)
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                });

            });
        }
    }
}
