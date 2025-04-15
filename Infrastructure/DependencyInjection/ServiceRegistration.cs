using Application.Interfaces.Repositories;
using Infrastructure.Persistence.Repositories;
using Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence.Contexts;
using Application.Features.Authentication.Commands;
using MediatR;
using Application.Provider;
using Domain.Settings;
using FluentValidation;
using Domain.Entities;
using Application.Utilities;
using Application.Features.Authentication.Otp.Commands;
using Application.Common;

namespace Infrastructure.DependencyInjection
{
    public class ServiceRegistration : IServicesRegistrationWithConfig
    {
        public void AddServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();  // Needed to access HttpContext

            services.Configure<JwtTokenSetting>(configuration.GetSection("JwtSettings"));

            // Register file settings
            services.AddSingleton<FileSettings>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var fileSettings = new FileSettings();
                config.GetSection("FileSettings").Bind(fileSettings);
                return fileSettings;
            });

            // Register file services
            services.AddTransient<IFileServices, FileServices>();

            // Register OtpService
            services.AddTransient<OtpService>();
        }
    }

    public class DatabaseRegistration : IDbServiceRegistration
    {
        public void AddServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<MainDbContext>(options =>
            {
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => sqlOptions.MigrationsAssembly(typeof(MainDbContext).Assembly.FullName));
            });
        }
    }

    public class RepositoryRegistration : IRepositoriesRegistration
    {
        public void AddServices(IServiceCollection services)
        {
            // Repositories are typically scoped because they interact with the database
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            // Register CQRS handlers with scoped lifetime
            services.AddScoped<IRequestHandler<RegisterCommand, IResult>, RegisterCommandHandler>();
           // services.AddScoped<IRequestHandler<LoginQuery, IResult>, LoginQueryHandler>();
            services.AddScoped<IRequestHandler<VerifyOtpCommand, Result>, VerifyOtpCommandHandler>();
        }
    }

    public class ServicesRegistration : IServicesRegistration
    {
        public void AddServices(IServiceCollection services)
        {
            // TokenService and EmailService are stateless, so they can be transient
            services.AddTransient<ITokenService, TokenService>();
            services.AddTransient<IEmailService, EmailService>();

            // Validators should be scoped or transient
            /*services.AddScoped<IValidator<RegisterCommand>, RegisterValidator>();
            services.AddScoped<IValidator<LoginQuery>, LoginQueryValidator>();*/

            // OtpSettings is a configuration setting, so it can be singleton
            services.AddScoped<OtpSettings>();

            // FileSettings is a configuration setting, so it can be singleton
            services.AddSingleton<FileSettings>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var fileSettings = new FileSettings();
                configuration.GetSection("FileSettings").Bind(fileSettings);
                return fileSettings;
            });

            // FileServices can be transient
            services.AddTransient<IFileServices, FileServices>();
        }
    }
}


