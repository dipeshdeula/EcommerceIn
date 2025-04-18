/*using Application.Interfaces.Repositories;
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
using Application.Features.Authentication.Queries.Login;
using Application.Features.Authentication.UploadImage.Commands;
using Application.Features.Authentication.Commands.UserInfo.Commands;
using Application.Features.Authentication.Queries.UserQuery;
using Application.Features.Authentication.Validation;

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
            services.AddScoped<JwtTokenSetting>();

            // Register CQRS handlers with scoped lifetime
            services.AddScoped<IRequestHandler<RegisterCommand, IResult>, RegisterCommandHandler>();
            services.AddScoped<IRequestHandler<LoginQuery, IResult>, LoginQueryHandler>();
            services.AddScoped<IRequestHandler<VerifyOtpCommand, Result<RegisterCommand>>, VerifyOtpCommandHandler>();
            services.AddScoped<IRequestHandler<GetAllUsersQuery, Result<IEnumerable<User>>>, GetAllUsersQueryHandler>();
            services.AddScoped<IRequestHandler<GetUsersQueryById,Result<User>>, GetUsersQueryByIdHandler>();
            services.AddScoped<IRequestHandler<UploadImageCommand,Result<User>>, UploadImageCommandHandler>();
            services.AddScoped<IRequestHandler<UsersUpdateCommand,Result<User>>, UsersUpdateCommandHandler>();
            services.AddScoped<IRequestHandler<SoftDeleteUserCommand,Result<User>>, SoftDeleteUserCommandHandler>();
            services.AddScoped<IRequestHandler<HardDeleteUserCommand,Result<User>>, HardDeleteUserCommandHandler>();
            services.AddScoped<IRequestHandler<UnDeleteUserCommnad,Result<User>>,UnDeleteUserCommandHandler>();
        }
    }

    public class ServicesRegistration : IServicesRegistration
    {
        public void AddServices(IServiceCollection services)
        {
            // TokenService and EmailService are stateless, so they can be transient
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IEmailService, EmailService>();

            // Validators should be scoped or transient
            services.AddScoped<IValidator<RegisterCommand>, RegisterValidator>();
            services.AddScoped<IValidator<LoginQuery>, LoginQueryValidator>();
           

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


*/

using Application.Common;
using Application.Dto;
using Application.Features.AddressFeat.Commands;
using Application.Features.AddressFeat.Queries;
using Application.Features.Authentication.Commands;
using Application.Features.Authentication.Commands.UserInfo.Commands;
using Application.Features.Authentication.Otp.Commands;
using Application.Features.Authentication.Queries.Login;
using Application.Features.Authentication.Queries.UserQuery;
using Application.Features.Authentication.UploadImage.Commands;
using Application.Features.CategoryFeat.Commands;
using Application.Interfaces.Repositories;
using Application.Utilities;
using Infrastructure.Persistence.Repositories;
using MediatR;

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
            services.AddScoped<IAddressRepository, AddressRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<JwtTokenSetting>();

            // Register CQRS handlers with scoped lifetime
            services.AddScoped<IRequestHandler<RegisterCommand, IResult>, RegisterCommandHandler>();
            services.AddScoped<IRequestHandler<LoginQuery, IResult>, LoginQueryHandler>();
            services.AddScoped<IRequestHandler<VerifyOtpCommand, Result<RegisterCommand>>, VerifyOtpCommandHandler>();
            services.AddScoped<IRequestHandler<GetAllUsersQuery, Result<IEnumerable<UserDTO>>>, GetAllUsersQueryHandler>();
            services.AddScoped<IRequestHandler<GetUsersQueryById, Result<User>>, GetUsersQueryByIdHandler>();
            services.AddScoped<IRequestHandler<UploadImageCommand, Result<User>>, UploadImageCommandHandler>();
            services.AddScoped<IRequestHandler<UsersUpdateCommand, Result<User>>, UsersUpdateCommandHandler>();
            services.AddScoped<IRequestHandler<SoftDeleteUserCommand, Result<User>>, SoftDeleteUserCommandHandler>();
            services.AddScoped<IRequestHandler<HardDeleteUserCommand, Result<User>>, HardDeleteUserCommandHandler>();
            services.AddScoped<IRequestHandler<UnDeleteUserCommnad, Result<User>>, UnDeleteUserCommandHandler>();

            services.AddScoped<IRequestHandler<AddressCommand, Result<AddressDTO>>, AddressCommandHandler>();
            services.AddScoped<IRequestHandler<GellAllAddressQuery, Result<IEnumerable<AddressDTO>>>, GetAllAddressQueryHandler>();
            services.AddScoped<IRequestHandler<GetAddressByUserId,Result<IEnumerable<AddressDTO>>>, GetAddressByUserIdHandler>();

            services.AddScoped<IRequestHandler<CreateCategoryCommand, Result<CategoryDTO>>, CreateCategoryCommandHandler>();
            services.AddScoped<IRequestHandler<CreateSubCategoryCommand,Result<SubCategoryDTO>>, CreateSubCategoryCommandHandler>();

           

            
        }
    }

    public class ServicesRegistration : IServicesRegistration
    {
        public void AddServices(IServiceCollection services)
        {
            // TokenService and EmailService are stateless, so they can be transient
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IEmailService, EmailService>();

           

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
