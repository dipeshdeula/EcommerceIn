/*using Application.Provider;
using Polly;
using Polly.Extensions.Http;

namespace Infrastructure.DependencyInjection
{
    public static class ServiceContainer
    {
        public static IServiceCollection AddPaymentServices(this IServiceCollection services, IConfiguration configuration)
        {
            // ✅ Register core payment services
            //services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();
            services.AddScoped<IPaymentSecurityService, PaymentSecurityService>();

            // ✅ Register payment providers
            services.AddScoped<EsewaProvider>();
            services.AddScoped<KhaltiProvider>();
            services.AddScoped<CODProvider>();

            // ✅ Configure HTTP clients with retry policies
            services.AddHttpClient("EsewaClient", client =>
            {
                client.BaseAddress = new Uri("https://rc-epay.esewa.com.np");
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "EcommerceApp/1.0");
            })
            .AddPolicyHandler(GetRetryPolicy());

            services.AddHttpClient("KhaltiClient", client =>
            {
                client.BaseAddress = new Uri("https://khalti.com/api/v2");
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "EcommerceApp/1.0");
            })
            .AddPolicyHandler(GetRetryPolicy());

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => !msg.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        Console.WriteLine($"⚠️ Payment API Retry {retryCount} after {timespan} seconds");
                    });
        }
    }
}
*/