using Application.Common.Behaviours;
using Application.Exceptions;
using Application.Features.CartItemFeat.Commands;
using Infrastructure.DependencyInjection;
using MediatR;

public class UserServiceManager : IServicesRegistration
{
    public void AddServices(IServiceCollection services)
    {
      

        // Carter validator locator lifetime
        services.AddScoped<Carter.IValidatorLocator, Carter.DefaultValidatorLocator>();

        //  MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateCartItemCommand).Assembly));

        //  Validation pipeline
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        //  Exception filter
        services.AddControllersWithViews(options =>
        {
            options.Filters.Add<ApiExceptionFilter>();
        });

        services.AddScoped<ApiExceptionFilter>();
        services.AddScoped<IStockReservationService, StockReservationService>();

    }
}
