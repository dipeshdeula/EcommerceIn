﻿using Application.Common;
using Application.Common.Models;
using Application.Dto;
using Application.Dto.AddressDTOs;
using Application.Dto.BannerEventSpecialDTOs;
using Application.Dto.BilItemDTOs;
using Application.Dto.CartItemDTOs;
using Application.Dto.CategoryDTOs;
using Application.Dto.CompanyDTOs;
using Application.Dto.OrderDTOs;
using Application.Dto.PaymentDTOs;
using Application.Dto.PaymentMethodDTOs;
using Application.Dto.ProductDTOs;
using Application.Dto.StoreDTOs;
using Application.Dto.UserDTOs;
using Application.Dto.WhishListDTOs;
using Application.Extension;
using Application.Features.AddressFeat.Commands;
using Application.Features.AddressFeat.Queries;
using Application.Features.Authentication.Commands;
using Application.Features.Authentication.Commands.UserInfo.Commands;
using Application.Features.Authentication.Otp.Commands;
using Application.Features.Authentication.Queries.Login;
using Application.Features.Authentication.Queries.UserQuery;
using Application.Features.Authentication.UploadImage.Commands;
using Application.Features.Authentication.Validation;
using Application.Features.BannerSpecialEvent.Commands;
using Application.Features.BannerSpecialEvent.DeleteCommands;
using Application.Features.BannerSpecialEvent.Queries;
using Application.Features.BillingItemFeat.Commands;
using Application.Features.BillingItemFeat.Queries;
using Application.Features.CartItemFeat.Commands;
using Application.Features.CartItemFeat.Queries;
using Application.Features.CategoryFeat.Commands;
using Application.Features.CategoryFeat.DeleteCommands;
using Application.Features.CategoryFeat.Queries;
using Application.Features.CategoryFeat.UpdateCommands;
using Application.Features.CompanyInfoFeat.Commands;
using Application.Features.CompanyInfoFeat.Queries;
using Application.Features.CustomAuthorization.Commands;
using Application.Features.ImageFeat.Queries;
using Application.Features.OrderFeat.Commands;
using Application.Features.OrderFeat.Queries;
using Application.Features.OrderFeat.UpdateCommands;
using Application.Features.PaymentMethodFeat.Commands;
using Application.Features.PaymentMethodFeat.DeleteCommands;
using Application.Features.PaymentMethodFeat.Queries;
using Application.Features.PaymentRequestFeat.Commands;
using Application.Features.PaymentRequestFeat.Queries;
using Application.Features.ProductFeat.Commands;
using Application.Features.ProductFeat.DeleteCommands;
using Application.Features.ProductFeat.Queries;
using Application.Features.ProductStoreFeat.Commands;
using Application.Features.ProductStoreFeat.Queries;
using Application.Features.StoreAddressFeat.Commands;
using Application.Features.StoreAddressFeat.Queries;
using Application.Features.StoreFeat.Commands;
using Application.Features.StoreFeat.DeleteCommands;
using Application.Features.StoreFeat.Queries;
using Application.Features.SubCategoryFeat.Commands;
using Application.Features.SubCategoryFeat.DeleteCommands;
using Application.Features.SubCategoryFeat.Queries;
using Application.Features.SubSubCategoryFeat.Commands;
using Application.Features.SubSubCategoryFeat.DeleteCommands;
using Application.Features.SubSubCategoryFeat.Queries;
using Application.Features.whishlistFeat.Commands;
using Application.Interfaces.Repositories;
using Application.Provider;
using Application.Utilities;
using FluentMigrator;
using FluentValidation;
using Infrastructure.Persistence.Messaging;
using Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using RabbitMQ.Client;
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

            // Register RabbitMQ connection and channel
            services.AddSingleton<IConnection>(sp =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = configuration["RabbitMQ:HostName"] ?? "localhost"
                };
                return factory.CreateConnection();
            });

            services.AddSingleton<IModel>(sp =>
            {
                var connection = sp.GetRequiredService<IConnection>();
                return connection.CreateModel();
            });


            // Register file services
            services.AddTransient<IFileServices, FileServices>();

            // Register OtpService
            services.AddTransient<OtpService>();
        }
    }

    /* public class DatabaseRegistration : IDbServiceRegistration
     {


         // In DatabaseRegistration.cs
         public void AddServices(IServiceCollection services, IConfiguration configuration)
         {
             var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

             if (environment == "Development")
             {
                 // Local SQL Server
                 var connectionString = configuration.GetConnectionString("DefaultConnection");
                 services.AddDbContext<MainDbContext>(options =>
                     options.UseNpgsql(connectionString));
             }
             else
             {
                 // Production PostgreSQL on Render
                 var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                                        Environment.GetEnvironmentVariable("DefaultConnection");
                 services.AddDbContext<MainDbContext>(options =>
                     options.UseNpgsql(connectionString));
             }
         }
     }*/

    public class DatabaseRegistration : IDbServiceRegistration
    {
        public void AddServices(IServiceCollection services, IConfiguration configuration)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                                  Environment.GetEnvironmentVariable("DefaultConnection");

            services.AddDbContext<MainDbContext>(options =>
            {
                options.UseNpgsql(connectionString, o =>
                {
                    // Connection resiliency
                    o.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null
                    );
                });

                // For development debugging
                if (environment == "Development")
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                    options.LogTo(Console.WriteLine);
                }
            });

            // ✅ Configure HTTP clients for payment gateways
            services.AddHttpClient("EsewaClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "GetInstantMart/1.0");
            })
            .AddPolicyHandler(GetRetryPolicy());

            services.AddHttpClient("KhaltiClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "GetInstantMart/1.0");
            })
            .AddPolicyHandler(GetRetryPolicy());

        }

        // ✅ Retry policy for HTTP clients
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
                        Console.WriteLine($"⚠️ HTTP Retry {retryCount} after {timespan} seconds");
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
            services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
            services.AddScoped<ISubSubCategoryRepository, SubSubCategoryRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductImageRepository, ProductImageRepository>();
            services.AddScoped<JwtTokenSetting>();
            services.AddScoped<IProductStoreRepository, ProductStoreRepository>();
            services.AddScoped<IStoreRepository, StoreRepository>();
            services.AddScoped<IStoreAddressRepository, StoreAddressRepository>();
            services.AddScoped<ICartItemRepository,CartItemRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderItemRepository, OrderItemRepository>();
            services.AddScoped<IBannerEventSpecialRepository, BannerEventSpecialRepository>();
            services.AddScoped<IBannerImageRepository, BannerImageRepository>();
            services.AddScoped<IPaymentMethodRepository,PaymentMethodRepository>();
            services.AddScoped<IPaymentRequestRepository, PaymentRequestRepository>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IEventProductRepository, EventProductRepository>();
            services.AddScoped<IEventUsageRepository, EventUsageRepository>();
            services.AddScoped<IEventRuleRepository, EventRuleRepository>();
            services.AddScoped<IProductPricingService, ProductPricingService>();
            services.AddScoped<ICurrentUserService,CurrentUserService>();
            services.AddScoped<IOtpService,OtpService>();

            services.AddScoped<ICartItemRepository, CartItemRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<ICompanyInfoRepository, CompanyInfoRepository>();

            services.AddScoped<IBillingRepository, BillingRepository>();
            services.AddScoped<IBillingItemRepository,BillingItemRepository>();

            // Register Authorization 
            services.AddScoped<IAuthorizationHandler, PermissionRequirementCommandHandler>();

            services.AddScoped<IWishlistRepository, WishlistRepository>();


            // Register CQRS handlers with scoped lifetime
            services.AddScoped<IRequestHandler<RegisterCommand, Result<UserDTO>>, RegisterCommandHandler>();

            services.AddScoped<IRequestHandler<LoginQuery, IResult>, LoginQueryHandler>();
            services.AddScoped<IRequestHandler<VerifyOtpCommand, Result<RegisterCommand>>, VerifyOtpCommandHandler>();
            services.AddScoped<IRequestHandler<GetAllUsersQuery, Result<IEnumerable<UserDTO>>>, GetAllUsersQueryHandler>();
            services.AddScoped<IRequestHandler<GetUsersQueryById, Result<UserDTO>>, GetUsersQueryByIdHandler>();
            services.AddScoped<IRequestHandler<UploadImageCommand, Result<User>>, UploadImageCommandHandler>();
            services.AddScoped<IRequestHandler<UsersUpdateCommand, Result<UserDTO>>, UsersUpdateCommandHandler>();
            services.AddScoped<IRequestHandler<SoftDeleteUserCommand, Result<User>>, SoftDeleteUserCommandHandler>();
            services.AddScoped<IRequestHandler<HardDeleteUserCommand, Result<User>>, HardDeleteUserCommandHandler>();
            services.AddScoped<IRequestHandler<UnDeleteUserCommnad, Result<User>>, UnDeleteUserCommandHandler>();

            services.AddScoped<IRequestHandler<AddressCommand, Result<AddressDTO>>, AddressCommandHandler>();
            services.AddScoped<IRequestHandler<GellAllAddressQuery, Result<IEnumerable<AddressDTO>>>, GetAllAddressQueryHandler>();
            services.AddScoped<IRequestHandler<GetAddressByUserId,Result<IEnumerable<AddressDTO>>>, GetAddressByUserIdHandler>();
            services.AddScoped<IRequestHandler<UpdateAddressCommand,Result<AddressDTO>>, UpdateAddressCommandHandler>();
            services.AddScoped<IRequestHandler<HardDeleteAddressCommand,Result<AddressDTO>>,HardDeleteAddressCommmandHandler>();

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

            services.AddScoped<IRequestHandler<SoftDeleteCategoryCommand,Result<CategoryDTO>>, SoftDeleteCategoryCommandHandler>();
            services.AddScoped<IRequestHandler<UnDeleteCategoryCommand,Result<CategoryDTO>>, UnDeleteCategoryCommandHandler>();
            services.AddScoped<IRequestHandler<HardDeleteCategoryCommand,Result<CategoryDTO>>, HardDeleteCategoryCommandHandler>();

            services.AddScoped<IRequestHandler<SoftDeleteSubCategoryCommand, Result<SubCategoryDTO>>, SoftDeleteSubCategoryCommandHandler>();
            services.AddScoped<IRequestHandler<UnDeleteSubCategoryCommand, Result<SubCategoryDTO>>, UnDeleteSubCategoryCommandHandler>();
            services.AddScoped<IRequestHandler<HardDeleteSubCategoryCommand, Result<SubCategoryDTO>>, HardDeleteSubCategoryCommandHandler>();

            services.AddScoped<IRequestHandler<SoftDeleteSubSubCategoryCommand, Result<SubSubCategoryDTO>>, SoftDeleteSubSubCategoryCommandHandler>();
            services.AddScoped<IRequestHandler<UnDeleteSubSubCategoryCommand, Result<SubSubCategoryDTO>>, UnDeleteSubSubCategoryCommandHandler>();
            services.AddScoped<IRequestHandler<HardDeleteSubSubCategoryCommand, Result<SubSubCategoryDTO>>, HardDeleteSubSubCategoryCommandHandler>();

            services.AddScoped<IRequestHandler<UpdateSubCategoryCommand, Result<SubCategoryDTO>>, UpdateSubCategoryCommandHandler>();
            services.AddScoped<IRequestHandler<UpdateProductCommand, Result<ProductDTO>>, UpdateProudctComamndHandler>();

            services.AddScoped<IRequestHandler<SoftDeleteProductCommand, Result<ProductDTO>>, SoftDeleteProductCommandHandler>();
            services.AddScoped<IRequestHandler<UnDeleteProductCommand, Result<ProductDTO>>, UnDeleteProductCommandHandler>();
            services.AddScoped<IRequestHandler<HardDeleteProductCommand, Result<ProductDTO>>, HardDeleteProductCommandHandler>();



            services.AddScoped<IRequestHandler<GetNearbyProductsQuery,Result<IEnumerable<NearbyProductDto>>>,GetNearbyProductQueryHandler>();
            services.AddScoped<IRequestHandler<CreateStoreCommand,Result<StoreDTO>>, CreateStoreCommandHandler>();
            services.AddScoped<IRequestHandler<GetAllStoreQuery, Result<IEnumerable<StoreDTO>>>, GetAllStoreQueryHandler>();
            services.AddScoped<IRequestHandler<GetProductsWithPricingQuery,Result<IEnumerable<ProductWithPricingDTO>>>, GetProductsWithPricingQueryHandler>();
            services.AddScoped<IRequestHandler<UpdateStoreCommand,Result<StoreDTO>>, UpdateStoreCommandHandler>();
            services.AddScoped<IRequestHandler<SoftDeleteStoreCommand,Result<StoreDTO>>, SoftDeleteStoreCommandHandler>();
            services.AddScoped<IRequestHandler<UnDeleteStoreCommand, Result<StoreDTO>>, UnDeleteStoreCommandHandler>();
            services.AddScoped<IRequestHandler<HardDeleteStoreCommand, Result<StoreDTO>>, HardDeleteStoreCommandHandler>();


            services.AddScoped<IRequestHandler<CreateStoreAddressCommand,Result<StoreAddressDTO>>, CreateStoreAddressCommandHandler>();
            services.AddScoped<IRequestHandler<GetStoreAddressByStoreIdQuery, Result<StoreAddressDTO>>, GetStoreAddressByStoreIdQueryHandler>();
            services.AddScoped<IRequestHandler<UpdateStoreAddressCommand, Result<StoreAddressDTO>>, UpdateStoreAddressCommandHandler>();

            services.AddScoped<IRequestHandler<CreateProductStoreCommand, Result<ProductStoreDTO>>, CreateProductStoreCommandHandler>();
            services.AddScoped<IRequestHandler<GetAllProductStoreQuery, Result<IEnumerable<ProductStoreDTO>>>, GetAllProductStoreQueryHandler>();
            services.AddScoped<IRequestHandler<GetAllProductByStoreIdQuery, Result<IEnumerable<StoreWithProductsDTO>>>, GetAllProductByStoreIdQueryHandler>();

            services.AddScoped<IRequestHandler<CreateCartItemCommand,Result<CartItemDTO>>, CreateCartItemCommandHandler>();
            services.AddScoped<IRequestHandler<GetAllCartItemQuery, Result<IEnumerable<CartItemDTO>>>, GetAllCartItemQueryHandler>();
            services.AddScoped<IRequestHandler<GetCartByUserIdQuery,Result<IEnumerable<CartItemDTO>>>, GetCartByUserIdQueryHandler>();
            services.AddScoped<IRequestHandler<UpdateCartItemCommand,Result<CartItemDTO>>, UpdateCartItemCommandHandler>();
            services.AddScoped<IRequestHandler<HardDeleteCartItemCommand,Result<CartItemDTO>>, HardDeleteCartItemCommandHandler>();

            services.AddScoped < IRequestHandler<GetAllProductsByCategoryId, Result<CategoryWithProductsDTO>>, GetAllProductsByCategoryIdHandler>();
            services.AddScoped<IRequestHandler<GetAllProductsBySubSubCategoryIdQuery, Result<CategoryWithProductsDTO>>, GetAllProductsBySubSubCategoryIdQueryHandler>();

            services.AddScoped<IRequestHandler<VerifyGoogleTokenCommand,IResult>, VerifyGoogleTokenCommandHandler>();

            services.AddScoped<IRequestHandler<CreatePlaceOrderCommand,Result<OrderDTO>>, CreatePlaceOrderCommandHandler>();
            services.AddScoped<IRequestHandler<GetAllOrderQuery,Result<IEnumerable<OrderDTO>>>, GetAllOrderQueryHandler>();
            services.AddScoped<IRequestHandler<GetOrderByUserIdQuery, Result<IEnumerable<OrderDTO>>>, GetOrderByUserIdQueryHandler>();
            services.AddScoped<IRequestHandler<UpdateOrderConfirmedCommand,Result<OrderConfirmationResponseDTO>>,UpdateOrderConfirmedCommandHandler>();

            services.AddScoped<IRequestHandler<CreateBannerSpecialEventCommand, Result<BannerEventSpecialDTO>>, CreateBannerSpecialEventCommandHandler>();
            services.AddScoped<IRequestHandler<GetAllBannerEventSpecialQuery, Result<PagedResult<BannerEventSpecialDTO>>>, GetAllBannerEventSpecialQueryHandler>();
            services.AddScoped<IRequestHandler<GetActiveBannerEventsQuery, Result<IEnumerable<BannerEventSpecialDTO>>>, GetActiveBannerEventsQueryHandler>();
            services.AddScoped<IRequestHandler<GetBannerEventByIdQuery, Result<BannerEventSpecialDTO>>, GetBannerEventByIdQueryHandler>();
            services.AddScoped<IRequestHandler<UpdateBannerSpecialEventCommand, Result<BannerEventSpecialDTO>>, UpdateBannerSpecialEventCommandHandler>();
            services.AddScoped<IRequestHandler<ActivateBannerEventCommand, Result<BannerEventSpecialDTO>>, ActivateBannerEventCommandHandler>();
            services.AddScoped<IRequestHandler<UploadBannerImageCommand, Result<IEnumerable<BannerImageDTO>>>, UploadBannerImageCommandHandler>();
            services.AddScoped<IRequestHandler<SoftDeleteBannerEventCommand, Result<BannerEventSpecialDTO>>, SoftDeleteBannerEventCommandHandler>();
            services.AddScoped<IRequestHandler<UnDeleteBannerEventCommand, Result<BannerEventSpecialDTO>>, UnDeleteBannerEventCommandHandler>();
            services.AddScoped<IRequestHandler<HardDeleteBannerEventCommand, Result<BannerEventSpecialDTO>>, HardDeleteBannerEventCommandHandler>();

            services.AddScoped<IRequestHandler<CreatePaymentMethodCommand, Result<PaymentMethodDTO>>, CreatePaymentMethodCommandHandler>();
            services.AddScoped<IRequestHandler<GetAllPaymentMethodQuery, Result<IEnumerable<PaymentMethodDTO>>>, GetAllPaymentMethodQueryHanlder>();
            services.AddScoped<IRequestHandler<UpdatePaymentMethodCommand, Result<PaymentMethodDTO>>, UpdatePaymentMethodCommandHandler>();
            services.AddScoped<IRequestHandler<SoftDeletePaymentMethodCommand, Result<PaymentMethodDTO>>, SoftDeletePaymentMethodCommandHandler>();
            services.AddScoped<IRequestHandler<UnDeletePaymentMethodCommand, Result<PaymentMethodDTO>>,UnDeletePaymentMethodCommandHandler>();
            services.AddScoped<IRequestHandler<HardDeletePaymentMethodCommand, Result<PaymentMethodDTO>>, HardDeletePaymentMethodCommandHandler>();

            services.AddScoped<IRequestHandler<GetImageQuery, Stream?>, GetImageQueryHandler>();

            services.AddScoped<IRequestHandler<CreatePaymentRequestCommand,Result<PaymentRequestDTO>>, CreatePaymentRequestCommandHandler>();
            services.AddScoped<IRequestHandler<GetAllPaymentQuery, Result<IEnumerable<PaymentRequestDTO>>>, GetAllPaymentQueryHandler>();
            services.AddScoped<IRequestHandler<GetPaymentByUserIdQuery, Result<IEnumerable<PaymentRequestDTO>>>,GetPaymentByUserIdQueryHandler>();
            services.AddScoped<IRequestHandler<VerifyPaymentCommand, Result<bool>>, VerifyPaymentCommandHandler>();

            services.AddScoped<IRequestHandler<CreateCompanyInfoCommand,Result<CompanyInfoDTO>>, CreateCompanyInfoCommandHandler>();
            services.AddScoped<IRequestHandler<GetAllCompanyInfoQuery, Result<IEnumerable<CompanyInfoDTO>>>, GetAllCompanyInfoQueryHandler>();
            services.AddScoped<IRequestHandler<UploadCompanyLogoCommand, Result<CompanyInfoDTO>>, UploadCompanyLogoCommandHandler>();

            services.AddScoped<IRequestHandler<CreateBillingItemCommand,Result<List<BillingItemDTO>>>, CreateBillingItemCommandHandler>();
            services.AddScoped<IRequestHandler<GetAllBillingItemQuery,Result<IEnumerable<BillingItemDTO>>>, GetAllBillingItemQueryHandler>();

            services.AddScoped<IRequestHandler<CreateWishlistCommand, Result<WishlistDTO>>, CreateWishlistCommandHandler>();
        }
    }

    public class ServicesRegistration : IServicesRegistration
    {
        public void AddServices(IServiceCollection services)
        {
            // TokenService and EmailService are stateless, so they can be transient
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IBannerEventAnalyticsService, BannerEventAnalyticsService>();
            services.AddScoped<IBannerEventRuleEngine, BannerEventRuleEngine>();
            services.AddSingleton<IRabbitMqConsumer, RabbitMQConsumer>();
            services.AddSingleton<IRabbitMqPublisher, RabbitMQPublisher>();
            services.AddScoped<RabbitMqConsumerService>();

            //for notif
            services.AddSingleton<IServiceTokenService, ServiceTokenService>();
            services.AddSingleton<INotificationRabbitMqPublisher,OrderPlacedPublisher>();
            services.AddSingleton<INotificationRabbitMqPublisher,OrderConfirmedPublisher>();

            // Register RabbitMqConsumer
            services.AddSingleton<OrderPlacedConsumer>();
            services.AddSingleton<OrderConfirmedConsumer>();
            services.AddHostedService<NotificationBackgroundConsumer>();

            services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
            services.AddValidatorsFromAssemblyContaining<AddressCommandValidator>();
            services.AddScoped<PaymentContextDto>();
            services.AddHostedService<EventPriceInvalidationService>();
            services.AddMemoryCache();

            services.AddSingleton<INepalTimeZoneService, NepalTimeZoneService>();

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

            // CartItem services
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<ICartStockService, CartStockService>();
            services.AddScoped<IBusinessConfigService, BusinessConfigService>();
            services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();
            services.AddScoped<IPaymentSecurityService, PaymentSecurityService>();
            // ✅ Register payment providers
            services.AddScoped<EsewaProvider>();
            services.AddScoped<KhaltiProvider>();
            services.AddScoped<CODProvider>();
            services.AddScoped<IEsewaService, EsewaService>();
        }
    }

        
    public class AuthorizationServiceRegistration : IServicesRegistration
    {
        public void AddServices(IServiceCollection services)
        {
            services.AddCustomAuthorization();
        }
    }

}
