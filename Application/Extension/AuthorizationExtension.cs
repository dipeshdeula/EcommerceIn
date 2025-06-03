using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Extension
{
    public static class AuthorizationExtension
    {
       
            public static void AddCustomAuthorization(this IServiceCollection services)
            {
                services.AddAuthorization(options =>
                {
                    // Policy for Admins and elevated roles
                    options.AddPolicy("RequireAdmin", policy =>
                        policy.RequireRole(
                            UserRoles.SuperAdmin.ToString(),
                            UserRoles.Admin.ToString()
                            
                        ));

                    options.AddPolicy("RequireDeliveryBoy", policy =>
                        policy.RequireRole(                         
                            UserRoles.DeliveryBoy.ToString()
                        ));

                    options.AddPolicy("RequireVendor", policy =>
                        policy.RequireRole(
                            UserRoles.Vendor.ToString()
                        ));


                    // Policy for custom permission claim
                    options.AddPolicy("CanManageProducts", policy =>
                        policy.RequireClaim("Permission", "ManageProducts"));
                });

                // Register custom handlers here if needed
                // services.AddSingleton<IAuthorizationHandler, YourCustomHandler>();
            }
        
    }
}
