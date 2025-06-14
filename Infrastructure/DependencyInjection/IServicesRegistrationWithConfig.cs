using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;

namespace Infrastructure.DependencyInjection
{
    public interface IServicesRegistrationWithConfig
    {
        void AddServices(IServiceCollection services, IConfiguration configuration);
    }

    public interface IServicesRegistration
    {
        void AddServices(IServiceCollection services);
    }

    public interface IDbServiceRegistration
    {
        void AddServices(IServiceCollection services, IConfiguration configuration);
    }

    public interface IRepositoriesRegistration
    {
        void AddServices(IServiceCollection services);
    }

    public interface IIdentityServicesRegistration
    {
        void AddServices(IServiceCollection services, IConfiguration configuration);
    }

    
}
