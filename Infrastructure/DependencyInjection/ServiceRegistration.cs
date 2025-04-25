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
using Application.Features.CategoryFeat.DeleteCategoryCommands;
using Application.Features.CategoryFeat.DeleteCommands;
using Application.Features.CategoryFeat.Queries;
using Application.Features.CategoryFeat.UpdateCommands;
using Application.Features.ProductFeat.Queries;
using Application.Features.ProductStoreFeat.Commands;
using Application.Features.StoreAddressFeat.Commands;
using Application.Features.StoreFeat.Commands;
using Application.Features.StoreFeat.Queries;
using Application.Interfaces.Repositories;
using Application.Utilities;
using Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Collections.Generic;

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

        // In DatabaseRegistration.cs
        /*public void AddServices(IServiceCollection services, IConfiguration configuration)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            if (environment == "Development")
            {
                // Local SQL Server
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                services.AddDbContext<MainDbContext>(options =>
                    options.UseSqlServer(connectionString));
            }
            else
            {
                // Production PostgreSQL on Render
                var connectionString = configuration.GetConnectionString("PostgresConnection") ??
                                       Environment.GetEnvironmentVariable("ConnectionStrings__PostgresConnection");
                services.AddDbContext<MainDbContext>(options =>
                    options.UseNpgsql(connectionString));
            }
        }*/
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
            services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
            services.AddScoped<ISubSubCategoryRepository, SubSubCategoryRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductImageRepository, ProductImageRepository>();
            services.AddScoped<JwtTokenSetting>();
            services.AddScoped<IProductStoreRepository, ProductStoreRepository>();
            services.AddScoped<IStoreRepository, StoreRepository>();
            services.AddScoped<IStoreAddressRepository, StoreAddressRepository>();


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
            services.AddScoped<IRequestHandler<CreateSubSubCategoryCommand,Result<SubSubCategoryDTO>>, CreateSubSubCategoryCommandHandler>();
            services.AddScoped<IRequestHandler<CreateProductCommand, Result<ProductDTO>>, CreateProductCommandHandler>();

            services.AddScoped<IRequestHandler<GetAllCategoryQuery,Result<IEnumerable<CategoryDTO>>>, GetAllCategoryQueryHandler>();
            services.AddScoped<IRequestHandler<GetAllSubCategoryQuery,  Result<IEnumerable <SubCategoryDTO>>>, GetAllSubCategoryHandler >();
            services.AddScoped<IRequestHandler<GetAllSubSubCategory, Result<IEnumerable<SubSubCategoryDTO>>>, GetAllSubSubCategoryHandler>();
            services.AddScoped <IRequestHandler<GetAllProductQuery, Result<IEnumerable<ProductDTO>>>, GetAllProductQueryHandler>();
            services.AddScoped<IRequestHandler<GetCategoryByIdQuery, Result<CategoryDTO>>, GetCategoryByIdQueryHandler>();
            services.AddScoped<IRequestHandler<GetSubCategoryByIdQuery, Result<SubCategoryDTO>>, GetSubCategoryByIdQueryHandler>();
            services.AddScoped<IRequestHandler<GetSubSubCategoryByIdQuery, Result<SubSubCategoryDTO>>, GetSubSubCategoryByIdQueryHandler>();
            services.AddScoped<IRequestHandler<GetProductByIdQuery, Result<ProductDTO>>, GetProductByIdQueryHandler>();

            services.AddScoped<IRequestHandler<UploadProductImagesCommand, Result<IEnumerable<ProductImageDTO>>>, UploadProductImagesCommandHandler>();

            services.AddScoped<IRequestHandler<UpdateCategoryCommand, Result<CategoryDTO>>, UpdateCategoryCommandHandler>();
            services.AddScoped<IRequestHandler<UpdateSubCategoryCommand, Result<SubCategoryDTO>>, UpdateSubCategoryCommandHandler>();
            services.AddScoped<IRequestHandler<UpdateProductCommand, Result<ProductDTO>>, UpdateProudctComamndHandler>();

            services.AddScoped<IRequestHandler<SoftDeleteProductCommand,Result<ProductDTO>>, SoftDeleteProductCommandHandler>();
            services.AddScoped<IRequestHandler<HardDeleteProductCommand, Result<ProductDTO>>, HardDeleteProductCommandHandler>();
            services.AddScoped<IRequestHandler<UnDeleteProductCommand, Result<ProductDTO>>, UnDeleteProductCommandHandler>();

            services.AddScoped<IRequestHandler<SoftDeleteCategoryCommand, Result<CategoryDTO>>, SoftDeleteCategoryCommandHandler>();

            services.AddScoped<IRequestHandler<GetNearbyProductsQuery,Result<IEnumerable<NearbyProductDto>>>,GetNearbyProductQueryHandler>();
            services.AddScoped<IRequestHandler<CreateStoreCommand,Result<StoreDTO>>, CreateStoreCommandHandler>();
            services.AddScoped<IRequestHandler<GetAllStoreQuery, Result<IEnumerable<StoreDTO>>>, GetAllStoreQueryHandler>();
            services.AddScoped<IRequestHandler<CreateStoreAddressCommand,Result<StoreAddressDTO>>, CreateStoreAddressCommandHandler>();

            services.AddScoped<IRequestHandler<CreateProductStoreCommand, Result<ProductStoreDTO>>, CreateProductStoreCommandHandler>();
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
