namespace Infrastructure.DependencyInjection
{
    public class LocationServiceRegistration
    {
        public void AddServices(IServiceCollection services, IConfiguration configuration)
        {
            //  Register HTTP clients
            services.AddHttpClient<IIPLocationService, IPLocationService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("User-Agent", "EcommerceApp/1.0");
            });

            services.AddHttpClient<IGoogleMapsService, GoogleMapsService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Add("User-Agent", "EcommerceApp/1.0");
            });

            // Register location services
            services.AddScoped<ILocationService, LocationService>();
            services.AddScoped<IIPLocationService, IPLocationService>();
            services.AddScoped<IGoogleMapsService, GoogleMapsService>();
        }
    }
}
