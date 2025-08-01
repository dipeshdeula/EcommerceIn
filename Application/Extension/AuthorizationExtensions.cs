using System.Security.Claims;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Extension
{
    public static class AuthorizationExtensions
    {
       
            public static void AddCustomAuthorization(this IServiceCollection services)
            {
                services.AddAuthorization(options =>
                {
                    // Policy for Admins (handles both "role" claim types)
                    options.AddPolicy("RequireAdmin", policy =>
               {
                   policy.RequireAssertion(context =>
                   {
                       var user = context.User;

                       // Check both "role" and ClaimTypes.Role
                       var roles = user.FindAll("role").Select(c => c.Value)
                                  .Concat(user.FindAll(ClaimTypes.Role).Select(c => c.Value))
                                  .ToList();

                       return roles.Any(role =>
                           role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                           role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                           role.Equals(UserRoles.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase) ||
                           role.Equals(UserRoles.Admin.ToString(), StringComparison.OrdinalIgnoreCase)
                       );
                   });
               });
                    // Policy for Delivery Boy
                    options.AddPolicy("RequireDeliveryBoy", policy =>
                {
                    policy.RequireAssertion(context =>
                    {
                        var user = context.User;
                        var roles = user.FindAll("role").Select(c => c.Value)
                                   .Concat(user.FindAll(ClaimTypes.Role).Select(c => c.Value))
                                   .ToList();

                        return roles.Any(role =>
                            role.Equals("DeliveryBoy", StringComparison.OrdinalIgnoreCase) ||
                            role.Equals(UserRoles.DeliveryBoy.ToString(), StringComparison.OrdinalIgnoreCase)
                        );
                    });
                });

                    // Policy for vendor

                    options.AddPolicy("RequireVendor", policy =>
                {
                    policy.RequireAssertion(context =>
                    {
                        var user = context.User;
                        var roles = user.FindAll("role").Select(c => c.Value)
                                   .Concat(user.FindAll(ClaimTypes.Role).Select(c => c.Value))
                                   .ToList();

                        return roles.Any(role =>
                            role.Equals("Vendor", StringComparison.OrdinalIgnoreCase) ||
                            role.Equals(UserRoles.Vendor.ToString(), StringComparison.OrdinalIgnoreCase)
                        );
                    });
                });

                    // Combined : Admin or Vendor Policy

                    options.AddPolicy("RequireAdminOrVendor", policy =>
               {
                   policy.RequireAssertion(context =>
                   {
                       var user = context.User;
                       var roles = user.FindAll("role").Select(c => c.Value)
                                  .Concat(user.FindAll(ClaimTypes.Role).Select(c => c.Value))
                                  .ToList();

                       return roles.Any(role =>
                           role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                           role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                           role.Equals("Vendor", StringComparison.OrdinalIgnoreCase) ||
                           role.Equals(UserRoles.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase) ||
                           role.Equals(UserRoles.Admin.ToString(), StringComparison.OrdinalIgnoreCase) ||
                           role.Equals(UserRoles.Vendor.ToString(), StringComparison.OrdinalIgnoreCase)
                       );
                   });
               });

                    options.AddPolicy("RequireAdminOrDeliveryBoy", policy =>
                    {
                        policy.RequireAssertion(context =>
                        {
                            var user = context.User;
                            var roles = user.FindAll("role").Select(c => c.Value)
                                       .Concat(user.FindAll(ClaimTypes.Role).Select(c => c.Value))
                                       .ToList();

                            return roles.Any(role =>
                                role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                                role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                                role.Equals("DeliveryBoy", StringComparison.OrdinalIgnoreCase) ||
                                role.Equals(UserRoles.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase) ||
                                role.Equals(UserRoles.Admin.ToString(), StringComparison.OrdinalIgnoreCase) ||
                                role.Equals(UserRoles.Vendor.ToString(), StringComparison.OrdinalIgnoreCase)
                            );
                        });
                    });





                    // Policy for custom permission claim
                    options.AddPolicy("CanManageProducts", policy =>
                        policy.RequireClaim("Permission", "ManageProducts"));
                        
                        // ✅ DEBUG: Policy to check any authenticated user
                options.AddPolicy("RequireAuthenticated", policy =>
                    policy.RequireAuthenticatedUser());
                });

               
            }
        
    }
}
