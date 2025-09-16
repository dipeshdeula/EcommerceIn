using Application.Interfaces.Services;
using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.TrackEventsFeat
{
    public class RedisCheckModule : CarterModule
    {
        public RedisCheckModule() : base("")
        {
            WithTags("test-redis");
            IncludeInOpenApi();
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("");

            //  SIMPLE: Quick Redis ping
            app.MapGet("/redis-ping", async (IHybridCacheService cacheService) =>
            {
                try
                {
                    var startTime = DateTime.UtcNow;
                    await cacheService.SetAsync("ping-test", DateTime.UtcNow.ToString(), TimeSpan.FromSeconds(30));
                    var result = await cacheService.GetAsync<string>("ping-test");
                    var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

                    return Results.Ok(new
                    {
                        status = "PONG",
                        latency_ms = elapsed,
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new
                    {
                        status = "FAILED",
                        error = ex.Message
                    });
                }
            });



            app.MapGet("/test-product-cache", async (IHybridCacheService cacheService) =>
            {
                try
                {
                    var testProductIds = new List<int> { 56, 55, 54, 10 };

                    // First call - should be miss and populate cache
                    var result1 = await cacheService.GetPricingBulkAsync(testProductIds, null);

                    // Second call - should hit cache
                    var result2 = await cacheService.GetPricingBulkAsync(testProductIds, null);

                    return Results.Ok(new
                    {
                        FirstCall = new
                        {
                            CacheHits = result1.Values.Count(v => v != null),
                            Total = testProductIds.Count,
                            Keys = result1.Keys.ToList()
                        },
                        SecondCall = new
                        {
                            CacheHits = result2.Values.Count(v => v != null),
                            Total = testProductIds.Count,
                            Keys = result2.Keys.ToList()
                        },
                        Message = "Second call should have higher hit rate"
                    });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { Error = ex.Message });
                }
            });


            //  UPDATED: test-redis endpoint with working health check
            app.MapGet("/test-redis", async (IHybridCacheService cacheService) =>
            {
                try
                {
                    var testResults = new
                    {
                        timestamp = DateTime.UtcNow,
                        tests = new List<object>()
                    };

                    //  TEST 1: Manual Health Check (using working method)
                    var healthSuccess = false;
                    var healthLatency = 0.0;
                    var healthDetails = "";

                    try
                    {
                        var healthStart = DateTime.UtcNow;
                        await cacheService.SetAsync("manual-health-test", "test-value", TimeSpan.FromMinutes(1));
                        var healthResult = await cacheService.GetAsync<string>("manual-health-test");
                        var healthEnd = DateTime.UtcNow;

                        healthSuccess = !string.IsNullOrEmpty(healthResult);
                        healthLatency = (healthEnd - healthStart).TotalMilliseconds;
                        healthDetails = healthSuccess ? "Redis is connected (manual test)" : "Redis connection failed (manual test)";

                        // Cleanup
                        try { await cacheService.RemoveAsync("manual-health-test"); } catch { }
                    }
                    catch (Exception healthEx)
                    {
                        healthSuccess = false;
                        healthLatency = 0;
                        healthDetails = $"Redis connection failed: {healthEx.Message}";
                    }

                    ((List<object>)testResults.tests).Add(new
                    {
                        test = "Health Check (Manual)",
                        success = healthSuccess,
                        latency = healthLatency,
                        details = healthDetails
                    });

                    if (!healthSuccess)
                    {
                        return Results.Ok(new
                        {
                            overall_status = "FAILED",
                            message = "Redis is not connected",
                            testResults
                        });
                    }

                    //  TEST 2: Write Test
                    var testKey = $"test-key-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
                    var testValue = "Hello Redis from .NET 8!";

                    var writeStart = DateTime.UtcNow;
                    await cacheService.SetAsync(testKey, testValue, TimeSpan.FromMinutes(2));
                    var writeTime = (DateTime.UtcNow - writeStart).TotalMilliseconds;

                    ((List<object>)testResults.tests).Add(new
                    {
                        test = "Cache Write",
                        success = true,
                        latency = writeTime,
                        details = $"Wrote key: {testKey}"
                    });

                    //  TEST 3: Read Test
                    var readStart = DateTime.UtcNow;
                    var retrievedValue = await cacheService.GetAsync<string>(testKey);
                    var readTime = (DateTime.UtcNow - readStart).TotalMilliseconds;

                    var readSuccess = retrievedValue == testValue;
                    ((List<object>)testResults.tests).Add(new
                    {
                        test = "Cache Read",
                        success = readSuccess,
                        latency = readTime,
                        details = readSuccess ? $"Retrieved: {retrievedValue}" : "Value mismatch"
                    });

                    //  TEST 4: Cleanup
                    try
                    {
                        await cacheService.RemoveAsync(testKey);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }

                    var allTestsPassed = ((List<object>)testResults.tests).All(t =>
                        (bool)((dynamic)t).success);

                    return Results.Ok(new
                    {
                        overall_status = allTestsPassed ? "SUCCESS" : "PARTIAL_SUCCESS",
                        message = allTestsPassed ? "All Redis tests passed!" : "Some tests failed",
                        redis_info = new
                        {
                            connected = healthSuccess,
                            latency_ms = healthLatency,
                            memory_usage = 0 // Will be populated when health method is fixed
                        },
                        testResults
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Redis test failed completely: {ex.Message}");
                }
            });
        }
    }
}
