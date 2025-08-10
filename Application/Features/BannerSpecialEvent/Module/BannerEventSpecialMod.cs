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
            app.MapGet("/analytics/real-time", async (
    ISender mediator,
    IBannerEventAnalyticsService analyticsService) =>
            {
                try
                {
                    var activeEvents = await mediator.Send(new GetActiveBannerEventsQuery());

                    if (!activeEvents.Succeeded)
                        return Results.BadRequest(activeEvents);

                    var realTimeStats = new List<object>();

                    foreach (var e in activeEvents.Data ?? new List<BannerEventSpecialDTO>())
                    {
                        //  Get real usage statistics
                        var usageStats = await analyticsService.GetEventUsageStatisticsAsync(e.Id);

                        var actualUsage = usageStats.Succeeded ? usageStats.Data.TotalUsages : 0;
                        var usagePercentage = e.MaxUsageCount > 0
                            ? Math.Round((decimal)actualUsage / e.MaxUsageCount * 100, 2)
                            : 0;

                        realTimeStats.Add(new
                        {
                            eventId = e.Id,
                            eventName = e.Name,
                            eventType = e.EventType.ToString(),
                            isActive = e.IsActive,
                            currentUsage = actualUsage,
                            maxUsage = e.MaxUsageCount,
                            usagePercentage = usagePercentage,
                            remainingUsage = Math.Max(0, e.MaxUsageCount - actualUsage),
                            timeStatus = e.TimeStatus,
                            daysRemaining = e.DaysRemaining,
                            priority = e.Priority,
                            totalDiscountGiven = usageStats.Succeeded ? usageStats.Data.TotalDiscount : 0,
                            uniqueUsers = usageStats.Succeeded ? usageStats.Data.UniqueUsers : 0,
                            performanceScore = usageStats.Succeeded ? usageStats.Data.PerformanceScore : 0,
                            status = DetermineEventStatus(e, actualUsage),
                            healthIndicator = GetHealthIndicator(usagePercentage, e.DaysRemaining),
                            //  Additional insights
                            averageDiscountPerUser = usageStats.Succeeded && usageStats.Data.UniqueUsers > 0
                                ? Math.Round(usageStats.Data.TotalDiscount / usageStats.Data.UniqueUsers, 2)
                                : 0,
                            engagementRate = usageStats.Succeeded && e.MaxUsageCount > 0
                                ? Math.Round((decimal)usageStats.Data.UniqueUsers / e.MaxUsageCount * 100, 2)
                                : 0
                        });
                    }

                    return Results.Ok(new
                    {
                        message = "Real-time monitoring data with accurate usage statistics",
                        timestamp = DateTime.UtcNow,
                        totalActiveEvents = realTimeStats.Count,
                        data = realTimeStats,
                        summary = new
                        {
                            totalEvents = realTimeStats.Count,
                            totalUsage = realTimeStats.Sum(s => (int)s.GetType().GetProperty("currentUsage")?.GetValue(s)!),
                            totalDiscountGiven = realTimeStats.Sum(s => (decimal)s.GetType().GetProperty("totalDiscountGiven")?.GetValue(s)!),
                            averagePerformanceScore = realTimeStats.Any()
                                ? Math.Round(realTimeStats.Average(s => (decimal)s.GetType().GetProperty("performanceScore")?.GetValue(s)!), 2)
                                : 0,
                            highPerformingEvents = realTimeStats.Count(s => GetHealthIndicator(
                                (decimal)(s.GetType().GetProperty("usagePercentage")?.GetValue(s) ?? 0m),
                                (int)(s.GetType().GetProperty("daysRemaining")?.GetValue(s) ?? 0)) == "Excellent"),
                            lowPerformingEvents = realTimeStats.Count(s => GetHealthIndicator(
                                (decimal)(s.GetType().GetProperty("usagePercentage")?.GetValue(s) ?? 0m),
                                (int)(s.GetType().GetProperty("daysRemaining")?.GetValue(s) ?? 0)) == "Poor")
                        }
                    });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { message = "Real-time monitoring failed", error = ex.Message });
                }
            })
            .RequireAuthorization("RequireAdminOrVendor")
            .WithName("GetRealTimeMonitoring")
            .WithSummary("Admin: Real-time event monitoring with accurate usage statistics")
            .Produces<object>(StatusCodes.Status200OK);

            // RULE ENGINE: Test event rules
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
                        recommendations = GenerateRuleTestRecommendations(ruleResult, eventResult.Data!)
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

            // ...rest of existing endpoints...


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

        private static string DetermineEventStatus(BannerEventSpecialDTO eventData, int actualUsage)
        {
            if (!eventData.IsActive) return "INACTIVE";
            if (eventData.IsExpired) return "EXPIRED";
            if (actualUsage >= eventData.MaxUsageCount) return "LIMIT_REACHED";
            if (eventData.DaysRemaining <= 1) return "ENDING_SOON";
            return "ACTIVE";
        }

        private static string GetHealthIndicator(decimal usagePercentage, int daysRemaining)
        {
            if (usagePercentage >= 80) return "Excellent";
            if (usagePercentage >= 50) return "Good";
            if (usagePercentage >= 20) return "Moderate";
            if (daysRemaining <= 1) return "Critical";
            return "Poor";
        }


        private static List<string> GenerateEventRecommendations(BannerEventSpecialDTO eventData)
        {
            var recommendations = new List<string>();

            // Usage-based recommendations
            if (eventData.UsagePercentage < 20)
            {
                recommendations.Add("Consider reducing restrictions or increasing promotion");
                recommendations.Add("Review target audience - rules might be too restrictive");
            }
            else if (eventData.UsagePercentage > 80)
            {
                recommendations.Add("High demand detected - consider increasing usage limits");
                recommendations.Add("Monitor closely to prevent early exhaustion");
            }

            // Rule-based recommendations
            if (eventData.TotalRulesCount > 3)
            {
                recommendations.Add("Complex rule structure may confuse customers");
                recommendations.Add("Consider simplifying rules for better user experience");
            }
            else if (eventData.TotalRulesCount == 0)
            {
                recommendations.Add("No targeting rules - event is open to all customers");
                recommendations.Add("Consider adding rules for better ROI control");
            }

            // Time-based recommendations
            if (eventData.DaysRemaining <= 1 && eventData.UsagePercentage < 50)
            {
                recommendations.Add("Event ending soon with low usage - consider extension");
                recommendations.Add("Increase marketing efforts for remaining time");
            }

            // Product-based recommendations
            if (eventData.TotalProductsCount < 5)
            {
                recommendations.Add("Limited product selection - consider adding more products");
            }

            return recommendations;
        }

        //  NEW: Method for generating recommendations from rule evaluation results
        private static List<string> GenerateRuleTestRecommendations(RuleEvaluationResultDTO ruleResult, BannerEventSpecialDTO eventData)
        {
            var recommendations = new List<string>();

            try
            {
                //  Rule evaluation specific recommendations
                if (ruleResult.IsEligible)
                {
                    recommendations.Add(" Cart meets all event requirements");
                    
                    if (!string.IsNullOrEmpty(ruleResult.FormattedDiscount))
                    {
                        recommendations.Add($"Discount applied: {ruleResult.FormattedDiscount}");
                    }
                    else
                    {
                        recommendations.Add($"Discount applied: Rs.{ruleResult.CalculatedDiscount:F2}");
                    }

                    if (ruleResult.AppliedRules?.Any() == true)
                    {
                        recommendations.Add($"Applied {ruleResult.AppliedRules.Count} rule(s) successfully");

                        foreach (var rule in ruleResult.AppliedRules)
                        {
                            recommendations.Add($"• Rule {rule.Priority}: {rule.Type} - {rule.FormattedDiscount}");
                        }
                    }
                    else
                    {
                        recommendations.Add("• Event base discount applied (no specific rules matched)");
                    }
                }
                else
                {
                    recommendations.Add("❌ Cart does not meet event requirements");

                    if (ruleResult.FailedRules?.Any() == true)
                    {
                        recommendations.Add("Failed rules analysis:");

                        foreach (var rule in ruleResult.FailedRules)
                        {
                            recommendations.Add($"• Rule {rule.Priority}: {rule.FailureReason}");

                            //  Specific suggestions based on failure reasons
                            if (!string.IsNullOrEmpty(rule.RequiredAction))
                            {
                                recommendations.Add($"  → {rule.RequiredAction}");
                            }
                            else
                            {
                                //  Fallback suggestions based on rule type and failure reason
                                if (rule.FailureReason.Contains("minimum order", StringComparison.OrdinalIgnoreCase))
                                {
                                    recommendations.Add($"  → Increase order value to at least Rs.{rule.MinOrderValue ?? 0}");
                                }
                                else if (rule.FailureReason.Contains("category", StringComparison.OrdinalIgnoreCase))
                                {
                                    recommendations.Add($"  → Add products from categories: {rule.TargetValue}");
                                }
                                else if (rule.FailureReason.Contains("product", StringComparison.OrdinalIgnoreCase))
                                {
                                    recommendations.Add($"  → Include specific products: {rule.TargetValue}");
                                }
                                else if (rule.FailureReason.Contains("payment", StringComparison.OrdinalIgnoreCase))
                                {
                                    recommendations.Add($"  → Use payment method: {rule.TargetValue}");
                                }
                                else
                                {
                                    recommendations.Add($"  → Check requirements: {rule.TargetValue}");
                                }
                            }
                        }
                    }
                    else
                    {
                        recommendations.Add("• No specific rule failures detected - check general event requirements");
                    }
                }

                //  Additional insights based on event data
                if (eventData?.MaxDiscountAmount.HasValue == true && ruleResult.CalculatedDiscount > eventData.MaxDiscountAmount.Value)
                {
                    recommendations.Add($" Calculated discount (Rs.{ruleResult.CalculatedDiscount:F2}) exceeds event cap (Rs.{eventData.MaxDiscountAmount:F2})");
                    recommendations.Add($"Final discount will be capped at Rs.{eventData.MaxDiscountAmount:F2}");
                }

                //  Performance insights
                if (ruleResult.ProcessingTimeMs > 1000)
                {
                    recommendations.Add(" Rule evaluation took longer than expected");
                    recommendations.Add("Consider optimizing rule complexity for better performance");
                }
                else if (ruleResult.ProcessingTimeMs > 0)
                {
                    recommendations.Add($"✓ Rule evaluation completed in {ruleResult.ProcessingTimeMs}ms");
                }

                //  Rules evaluation summary
                if (ruleResult.RulesEvaluated > 0)
                {
                    recommendations.Add($"📊 Summary: {ruleResult.RulesEvaluated} rules evaluated, {ruleResult.AppliedRules?.Count ?? 0} applied, {ruleResult.FailedRules?.Count ?? 0} failed");
                }

                //  Fallback if no recommendations were generated
                if (recommendations.Count == 0)
                {
                    recommendations.Add("Rule evaluation completed - check evaluation details for more information");
                }
            }
            catch (Exception)
            {
                //  Error handling - provide basic fallback recommendations
                recommendations.Clear();
                recommendations.Add(" Error generating detailed recommendations");
                recommendations.Add(ruleResult.IsEligible ? " Cart is eligible for discount" : "❌ Cart is not eligible for discount");
                
                if (ruleResult.CalculatedDiscount > 0)
                {
                    recommendations.Add($"Discount amount: Rs.{ruleResult.CalculatedDiscount:F2}");
                }
            }

            return recommendations;
        }
        private static object GenerateRuleReport(BannerEventSpecialDTO eventData)
        {
            return new
            {
                eventInfo = new
                {
                    name = eventData.Name ?? "Unknown Event",
                    type = eventData.EventType.ToString(),
                    status = eventData.Status.ToString(),
                    isActive = eventData.IsActive,
                    tagLine = eventData.TagLine ?? "",
                    priority = eventData.Priority
                },
                discountStructure = new
                {
                    baseDiscount = eventData.FormattedDiscount ?? "No discount",
                    maxDiscountCap = eventData.MaxDiscountAmount.HasValue ? $"Rs.{eventData.MaxDiscountAmount}" : "No cap",
                    minOrderValue = eventData.MinOrderValue.HasValue ? $"Rs.{eventData.MinOrderValue}" : "No minimum",
                    promotionType = eventData.PromotionType.ToString()
                },
                ruleAnalysis = new
                {
                    totalRules = eventData.TotalRulesCount,
                    hasRestrictions = eventData.TotalRulesCount > 0,
                    complexity = eventData.TotalRulesCount switch
                    {
                        0 => "No restrictions - Open to all customers",
                        1 => "Simple targeting - Single criterion",
                        <= 3 => "Moderate targeting - Multiple criteria",
                        _ => "Complex targeting - Advanced rule system"
                    },
                    //  Safe rule breakdown with null checks
                    ruleBreakdown = eventData.Rules?.Select(r => new
                    {
                        ruleId = r.Id,
                        priority = r.Priority,
                        type = r.Type.ToString(),
                        description = r.RuleDescription ?? "No description",
                        restriction = r.FormattedMinOrder ?? "No restriction",
                        discount = r.FormattedDiscount ?? "No discount",
                        maxDiscount = r.FormattedMaxDiscount ?? "No limit",
                        isRestrictive = r.IsRestrictive,
                        targetAudience = r.TargetAudience ?? "General"
                    }).ToList()
                },
                usageAnalysis = new
                {
                    currentUsage = eventData.CurrentUsageCount,
                    maxUsage = eventData.MaxUsageCount,
                    usagePercentage = eventData.UsagePercentage,
                    remainingCapacity = eventData.RemainingUsage,
                    usageStatus = eventData.UsagePercentage switch
                    {
                        >= 80 => "High demand - Consider increasing capacity",
                        >= 50 => "Moderate usage - Performing well",
                        >= 20 => "Low usage - May need promotion",
                        _ => "Very low usage - Review targeting"
                    },
                    //  Safe usage insights with null checks
                    usageTrend = eventData.UsageSummary != null ? new
            {
                totalDiscount = eventData.UsageSummary.FormattedTotalDiscount ?? "Rs.0.00",
                uniqueUsers = eventData.UsageSummary.UniqueUsers,
                averageDiscount = eventData.UsageSummary.FormattedAverageDiscount ?? "Rs.0.00",
                conversionRate = $"{eventData.UsageSummary.ConversionRate}%",
                frequency = eventData.UsageSummary.UsageFrequency ?? "No usage",
                message = "Usage data available"
            } : new { 
                totalDiscount = "Rs.0.00",
                uniqueUsers = 0,
                averageDiscount = "Rs.0.00",
                conversionRate = "0%",
                frequency = "No usage",
                message = "No usage data available" 
            }
                },
                timeAnalysis = new
                {
                    timeStatus = eventData.TimeStatus ?? "Unknown",
                    daysRemaining = eventData.DaysRemaining,
                    isExpired = eventData.IsExpired,
                    isCurrentlyActive = eventData.IsCurrentlyActive,
                    dateRange = eventData.FormattedDateRange ?? eventData.FormattedDateRange ?? "Date range not available",
                    timezone = eventData.TimeZoneInfo?.TimeZoneName ?? "UTC"
                },
                productAnalysis = new
                {
                    totalProducts = eventData.TotalProductsCount,
                    productIds = eventData.ProductIds ?? new List<int>(),
                    hasProductSpecificDiscounts = eventData.EventProducts?.Any(p => p.HasSpecificDiscount) ?? false,
                    productBreakdown = eventData.EventProducts?.Select(p => new
                    {
                        productId = p.ProductId,
                        productName = p.ProductName ?? "Unknown Product",
                        category = p.CategoryName ?? "Uncategorized",
                        originalPrice = p.FormattedOriginalPrice ?? "Rs.0.00",
                        discountedPrice = p.FormattedDiscountPrice ?? "Rs.0.00",
                        savings = p.FormattedSavings ?? "No savings",
                        hasSpecificDiscount = p.HasSpecificDiscount
                    }).ToList() 
                },
                recommendations = GenerateEventRecommendations(eventData)
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
