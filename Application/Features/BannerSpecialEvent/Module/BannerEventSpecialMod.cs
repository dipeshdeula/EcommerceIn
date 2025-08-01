using Application.Common.Models;
using Application.Dto;
using Application.Dto.BannerEventSpecialDTOs;
using Application.Extension;
using Application.Features.BannerSpecialEvent.Commands;
using Application.Features.BannerSpecialEvent.DeleteCommands;
using Application.Features.BannerSpecialEvent.Queries;
using Application.Interfaces.Services;
using Carter;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.BannerSpecialEvent.Module
{
    public class BannerEventSpecialMod : CarterModule
    {
        public BannerEventSpecialMod() : base("")
        {
            WithTags("BannerEventSpecial");
            IncludeInOpenApi();

        }
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("api/banner-events")
                .WithTags("Banner Event Special");


            app.MapPost("/create", async (ISender mediator,
               CreateBannerSpecialEventDTO bannerSpecialEventDTO,
               IBannerEventRuleEngine ruleEngine
              ) =>
            {
                var command = new CreateBannerSpecialEventCommand(bannerSpecialEventDTO);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return await Task.FromResult(Results.Created($"/api/banner-events/{result.Data?.Id}", new { result.Message, result.Data }));
            }).RequireAuthorization("RequireAdminOrVendor")
                .DisableAntiforgery()
               .Accepts<CreateBannerSpecialEventCommand>("application/json")
               .Produces<BannerEventSpecialDTO>(StatusCodes.Status200OK)
               .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
               .WithName("CreateBannerEvent")
                .WithSummary("Create a new banner event")
                .WithDescription("Creates a new banner event with optional rules and product associations");


            // Get active banner events for customers
            app.MapGet("/active", async (ISender mediator) =>
            {
                var query = new GetActiveBannerEventsQuery();
                var result = await mediator.Send(query);
                return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("GetActiveBannerEvents")
            .WithSummary("Get active banner events for customers")
            .WithDescription("Returns currently active promotional events visible to customers")
            .Produces<List<BannerEventSpecialDTO>>(StatusCodes.Status200OK);

            // Get all banner events with filtering and pagination
            app.MapGet("/", async (
                ISender mediator,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 10,
                [FromQuery] bool includeDeleted = false,
                [FromQuery] string? status = null,
                [FromQuery] bool? isActive = null,
                [FromQuery] string? eventType = null,
                [FromQuery] DateTime? startDate = null,
                [FromQuery] DateTime? endDate = null
                ) =>
            {
                var query = new GetAllBannerEventSpecialQuery(
                    PageNumber: pageNumber,
                    PageSize: pageSize,
                    IncludeDeleted: includeDeleted,
                    Status: status,
                    IsActive: isActive
                );

                var result = await mediator.Send(query);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new
                    {
                        message = result.Message,
                        errors = result.Errors
                    });
                }

                return Results.Ok(new
                {
                    message = result.Message,
                    data = result.Data
                });
            })
            .Produces<PagedResult<BannerEventSpecialDTO>>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .WithName("GetAllBannerEvents")
            .WithSummary("Get all banner events")
            .WithDescription("Retrieves paginated list of banner events with optional filtering");

            // Get banner event by ID
            app.MapGet("/getEventById", async (ISender mediator, [FromQuery] int bannerId) =>
            {
                var command = new GetBannerEventByIdQuery(bannerId);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            //  Validate cart against event rules
            app.MapPost("/validate-cart", async (
                int id,
                [FromBody] CartValidationRequestDTO request,
                ISender mediator,
                IBannerEventRuleEngine ruleEngine) =>
            {
                try
                {
                    var isValid = await ruleEngine.ValidateCartRulesAsync(
                        id, request.CartItems, request.User!, request.PaymentMethod);

                    return Results.Ok(new
                    {
                        isValid,
                        message = isValid ? "Cart is valid for this event" : "Cart doesn't meet event requirements"
                    });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { message = "Validation failed", error = ex.Message });
                }
            })
            .WithName("ValidateCartForEvent")
            .WithSummary("Validate if cart meets event rules")
            .Produces<object>(StatusCodes.Status200OK);

            app.MapPost("/UploadBannerImage", async (ISender mediator, [FromForm] int bannerId, [FromForm] IFormFileCollection files) =>
            {
                var command = new UploadBannerImageCommand(bannerId, files);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            })
               .RequireAuthorization("RequireAdminOrVendor")
               .DisableAntiforgery()
               .Accepts<UploadBannerImageCommand>("multipart/form-data")
               .Produces<IEnumerable<BannerImageDTO>>(StatusCodes.Status200OK)
               .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);
            ;

            app.MapPut("/activateOrDeactivate", async (
                int BannerEventId,
                bool IsActive,
                ISender mediator) =>
            {
                var command = new ActivateBannerEventCommand(
                    BannerEventId, IsActive);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });

            }).RequireAuthorization("RequireAdminOrVendor")
            .WithName("ActivateBannerEvent")
            .WithSummary("Activate or deactivate a banner event")
            .WithDescription("Automatically recalculates product prices when activated/deactivated");

            app.MapGet("/analytics", async (
                int Id,
                [FromQuery] DateTime? fromDate,
                [FromQuery] DateTime? toDate,
                IBannerEventAnalyticsService analyticsService
            ) =>
            {
                try
                {
                    var performance = await analyticsService.GetEventPerformanceAsync(Id, fromDate, toDate);
                    return Results.Ok(new
                    {
                        message = "Performance report generated successfully",
                        data = performance
                    });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { message = "Analytics failed", error = ex.Message });
                }

            }).RequireAuthorization("RequireAdminOrVendor")
            .WithName("GetEventAnalytics")
            .WithSummary("Admin: Get event performance analytics")
            .Produces<EventPerformanceReportDTO>(StatusCodes.Status200OK);

            // analytics : top performing events
            app.MapGet("/analytics/top-performing", async (
                IBannerEventAnalyticsService analyticsService,
                [FromQuery] int count = 10) =>
            {
                try
                {
                    var topEvents = await analyticsService.GetTopPerformingEventsAsync(count);
                    return Results.Ok(new
                    {
                        message = $"Top {count} performing events retrieved",
                        data = topEvents
                    });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { message = "Analytics failed", error = ex.Message });
                }

            }).RequireAuthorization("RequireAdminOrVendor")
            .WithName("GetTopPerformingEvents")
            .WithSummary("Admin: Get top performing events")
            .Produces<List<EventUsageStatisticsDTO>>(StatusCodes.Status200OK);

            // analytics : total discount summary
            app.MapGet("/analytics/discount-summary", async (
               [FromQuery] DateTime? fromDate,
               [FromQuery] DateTime? toDate,
               IBannerEventAnalyticsService analyticsService) =>
           {
               try
               {
                   var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
                   var to = toDate ?? DateTime.UtcNow;

                   var totalDiscount = await analyticsService.GetTotalDiscountsGivenAsync(from, to);

                   return Results.Ok(new
                   {
                       message = "Discount summary calculated",
                       data = new
                       {
                           fromDate = from,
                           toDate = to,
                           totalDiscountGiven = totalDiscount,
                           averageDiscountPerDay = totalDiscount / Math.Max(1, (to - from).Days),
                           period = $"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}"
                       }
                   });
               }
               catch (Exception ex)
               {
                   return Results.BadRequest(new { message = "Summary calculation failed", error = ex.Message });
               }
           })
           .RequireAuthorization("RequireAdminOrVendor")
           .WithName("GetDiscountSummary")
           .WithSummary("Admin: Get total discount summary")
           .Produces<object>(StatusCodes.Status200OK);

            // analytics : Real-time event monitoring
            app.MapGet("/analytics/real-time", async (ISender mediator) =>
           {
               try
               {
                   var activeEvents = await mediator.Send(new GetActiveBannerEventsQuery());

                   if (!activeEvents.Succeeded)
                       return Results.BadRequest(activeEvents);

                   var realTimeStats = activeEvents.Data?.Select(e => new
                   {
                       eventId = e.Id,
                       eventName = e.Name,
                       eventType = e.EventType.ToString(),
                       isActive = e.IsActive,
                       usagePercentage = e.UsagePercentage,
                       remainingUsage = e.RemainingUsage,
                       timeStatus = e.TimeStatus,
                       daysRemaining = e.DaysRemaining,
                       priority = e.Priority
                   }).ToList();

                   return Results.Ok(new
                   {
                       message = "Real-time monitoring data",
                       timestamp = DateTime.UtcNow,
                       data = realTimeStats
                   });
               }
               catch (Exception ex)
               {
                   return Results.BadRequest(new { message = "Real-time monitoring failed", error = ex.Message });
               }
           })
           .RequireAuthorization("RequireAdminOrVendor")
           .WithName("GetRealTimeMonitoring")
           .WithSummary("Admin: Real-time event monitoring")
           .Produces<object>(StatusCodes.Status200OK);

            // 🛠️ RULE ENGINE: Test event rules
            app.MapPost("/test-rules", async (
                int id,
                [FromBody] RuleTestRequestDTO request,
                IBannerEventRuleEngine ruleEngine,
                ISender mediator) =>
            {
                try
                {
                    // Get the banner event
                    var eventQuery = new GetBannerEventByIdQuery(id);
                    var eventResult = await mediator.Send(eventQuery);

                    if (!eventResult.Succeeded)
                        return Results.NotFound(new { message = "Event not found" });

                    // Create evaluation context
                    var context = new EvaluationContextDTO
                    {
                        CartItems = request.TestCartItems ?? new List<CartItem>(),
                        User = request.TestUser,
                        PaymentMethod = request.TestPaymentMethod,
                        OrderTotal = request.TestOrderTotal
                    };

                    // Test rules using your existing rule engine
                    var ruleResult = await ruleEngine.EvaluateAllRulesAsync(eventResult.Data.ToEntity(), context);

                    return Results.Ok(new
                    {
                        message = "Rule test completed",
                        eventId = id,
                        eventName = eventResult.Data?.Name,
                        testResult = ruleResult,
                        recommendations = GenerateRuleRecommendations(ruleResult)
                    });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { message = "Rule test failed", error = ex.Message });
                }
            })
            .RequireAuthorization("RequireAdminOrVendor")
            .WithName("TestEventRules")
            .WithSummary("Admin: Test banner event rules")
            .WithDescription("Test how event rules behave with sample cart data")
            .Produces<object>(StatusCodes.Status200OK);

            //  RULE ENGINE: Get rule validation report
            app.MapGet("/rule-report", async (
                int id,
                ISender mediator) =>
            {
                try
                {
                    var eventQuery = new GetBannerEventByIdQuery(id);
                    var eventResult = await mediator.Send(eventQuery);

                    if (!eventResult.Succeeded)
                        return Results.NotFound(new { message = "Event not found" });

                    var report = GenerateRuleReport(eventResult.Data);

                    return Results.Ok(new
                    {
                        message = "Rule report generated",
                        eventId = id,
                        data = report
                    });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { message = "Report generation failed", error = ex.Message });
                }
            })
            .RequireAuthorization("RequireAdminOrVendor")
            .WithName("GetEventRuleReport")
            .WithSummary("Admin: Get event rule analysis report")
            .Produces<object>(StatusCodes.Status200OK);



            app.MapDelete("softDeleteBannerEvent", async (int BannerId, ISender mediator) =>
            {
                var command = new SoftDeleteBannerEventCommand(BannerId);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Data });

                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization("RequireAdminOrVendor");

            app.MapDelete("UnDeleteBannerEvent", async (int BannerId, ISender mediator) =>
            {
                var command = new UnDeleteBannerEventCommand(BannerId);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Data });
                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization("RequireAdminOrVendor");

            app.MapDelete("HardDeleteBannerEvent", async (int BannerId, ISender mediator) =>
            {
                var command = new HardDeleteBannerEventCommand(BannerId);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Data });
                return Results.Ok(new { result.Message, result.Data });

            }).RequireAuthorization("RequireAdminOrVendor");


        }
        
         private object GenerateRuleRecommendations(RuleEvaluationResultDTO ruleResult)
        {
            var recommendations = new List<string>();

            if (!ruleResult.IsValid)
            {
                recommendations.Add("Event rules are restricting access");
                recommendations.Add("Consider adjusting rule criteria for better customer reach");
            }
            else
            {
                recommendations.Add("Rules are working correctly");
                recommendations.Add("Event is accessible to target customers");
            }

            return new
            {
                status = ruleResult.IsValid ? "PASS" : "FAIL",
                recommendations,
                failedRuleCount = ruleResult.FailedRules?.Count ?? 0
            };
        }

        private object GenerateRuleReport(BannerEventSpecialDTO eventData)
        {
            return new
            {
                eventInfo = new
                {
                    name = eventData.Name,
                    type = eventData.EventType.ToString(),
                    status = eventData.Status.ToString(),
                    isActive = eventData.IsActive
                },
                ruleAnalysis = new
                {
                    totalRules = eventData.TotalRulesCount,
                    hasRestrictions = eventData.TotalRulesCount > 0,
                    complexity = eventData.TotalRulesCount switch
                    {
                        0 => "No restrictions",
                        1 => "Simple targeting",
                        <= 3 => "Moderate targeting", 
                        _ => "Complex targeting"
                    }
                },
                usageAnalysis = new
                {
                    currentUsage = eventData.CurrentUsageCount,
                    maxUsage = eventData.MaxUsageCount,
                    usagePercentage = eventData.UsagePercentage,
                    remainingCapacity = eventData.RemainingUsage
                },
                timeAnalysis = new
                {
                    timeStatus = eventData.TimeStatus,
                    daysRemaining = eventData.DaysRemaining,
                    isExpired = eventData.IsExpired,
                    isCurrentlyActive = eventData.IsCurrentlyActive
                }
            };
        }
    }

    // SUPPORTING DTOs
    public class CartValidationRequestDTO
    {
        public List<CartItem> CartItems { get; set; } = new();
        public User? User { get; set; }
        public string? PaymentMethod { get; set; }
    }

    public class RuleTestRequestDTO
    {
        public List<CartItem>? TestCartItems { get; set; }
        public User? TestUser { get; set; }
        public string? TestPaymentMethod { get; set; }
        public decimal? TestOrderTotal { get; set; }
    }
}
