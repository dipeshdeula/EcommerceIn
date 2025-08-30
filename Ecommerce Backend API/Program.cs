using Application.Extension;
using Application.Interfaces.Services;
using Application.Provider;
using Carter;
using Infrastructure.DependencyInjection;
using Infrastructure.Hubs;
using Infrastructure.Middleware.Security;
using Infrastructure.Persistence.Configurations;
using Infrastructure.Persistence.Contexts;
using Infrastructure.Persistence.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text.Json.Serialization;

EnvProvider.LoadEnv();
var builder = WebApplication.CreateBuilder(args);

// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ListenAnyIP(5000); // HTTP
//     options.ListenAnyIP(5001, listenOptions =>
//     {
//         listenOptions.UseHttps(); // HTTPS
//     });
// });



//  Validate Google Maps API Key
var googleMapsApiKey = builder.Configuration["LocationSettings:GoogleMapsApiKey"];
if (string.IsNullOrEmpty(googleMapsApiKey))
{
    Console.WriteLine(" Google Maps API Key not configured - some location features will be limited");
}
else
{
    Console.WriteLine("Google Maps API Key configured");
}

// Configure HybridCache options
builder.Services.Configure<HybridCacheOptions>(
    builder.Configuration.GetSection(HybridCacheOptions.SectionName));

// Memory cache configuration
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // 1024 units (approx 1GB with our size calculation)
    options.CompactionPercentage = 0.25; // Remove 25% when limit reached
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(2);

    options.TrackStatistics = true;
});

// Redis Configuration
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "EcommerceAPI";
    });

    // Redis connection multiplexer for advanced operations
    builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
    {
        var configuration = ConfigurationOptions.Parse(redisConnectionString);
        configuration.AbortOnConnectFail = false; // Don't fail startup if Redis is down
        configuration.ConnectRetry = 3;
        configuration.ConnectTimeout = 5000;
        configuration.SyncTimeout = 5000;
        return ConnectionMultiplexer.Connect(configuration);
    });

}
else
{
   
    Console.WriteLine(" Redis not configured - using mock cache service");
}

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EcommerceInstant API",
        Version = "v1",
        Description = "This API is responsible for overall data distribution and authorization."
    });

    // Add JWT Bearer
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


builder.Services.AddAuthenticationExtension(builder.Configuration);
builder.Services.AddCarterExtension();
builder.Services.AddCorsExtension();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();

//  Register Location Services
new LocationServiceRegistration().AddServices(builder.Services, builder.Configuration);

// Configure JSON serializer to handle reference loops and increase maximum depth
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
    options.JsonSerializerOptions.MaxDepth = 128; // Increase the maximum depth
});

//for signalr
builder.Services.Configure<ApiConfig>(builder.Configuration.GetSection("ApiSettings"));


// Validate Khalti and eSewa configurations
var khaltiKey = builder.Configuration["PaymentGateways:Khalti:SecretKey"];
var khaltiBaseUrl = builder.Configuration["PaymentGateways:Khalti:BaseUrl"];
var esewaMerchantId = builder.Configuration["PaymentGateways:Esewa:MerchantId"];
var esewaBaseUrl = builder.Configuration["PaymentGateways:Esewa:BaseUrl"];
var esewaSecret = builder.Configuration["PaymentGateways:Esewa:SecretKey"];

/*Console.WriteLine($"Khalti Key: {khaltiKey}");
Console.WriteLine($"Khalti Base URL: {khaltiBaseUrl}");
Console.WriteLine($"eSewa Merchant ID: {esewaMerchantId}");
Console.WriteLine($"eSewa Base URL: {esewaBaseUrl}");
Console.WriteLine($"eSewa Secret Key: {esewaSecret}");*/

//  Skip validation in Test environment
if (!builder.Environment.IsEnvironment("Test"))
{
    // Only validate in non-test environments
    if (string.IsNullOrEmpty(khaltiKey) || string.IsNullOrEmpty(khaltiBaseUrl))
    {
        throw new InvalidOperationException("Khalti SecretKey or BaseUrl is not configured in appsettings.json");
    }

    if (string.IsNullOrEmpty(esewaMerchantId) || string.IsNullOrEmpty(esewaBaseUrl))
    {
        throw new InvalidOperationException("eSewa MerchantId or BaseUrl is not configured in appsettings.json");
    }
}

// if (string.IsNullOrEmpty(khaltiKey) || string.IsNullOrEmpty(khaltiBaseUrl))
// {
//     throw new InvalidOperationException("Khalti SecretKey or BaseUrl is not configured in appsettings.json");
// }

// if (string.IsNullOrEmpty(esewaMerchantId) || string.IsNullOrEmpty(esewaBaseUrl))
// {
//     throw new InvalidOperationException("eSewa MerchantId or BaseUrl is not configured in appsettings.json");
// }

// Optional: Configure named HTTP clients for Khalti and eSewa (if needed)
builder.Services.AddHttpClient("KhaltiClient", client =>
{
    client.BaseAddress = new Uri(khaltiBaseUrl!);
    client.DefaultRequestHeaders.Add("Authorization", $"Key {khaltiKey}");
});

builder.Services.AddHttpClient("EsewaClient", client =>
{
    client.BaseAddress = new Uri(esewaBaseUrl!);
});


//  basic rate limiting for API endpoints
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Basic global rate limit (CloudFlare handles the heavy lifting)
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.PermitLimit = 100; // 100 requests per minute per IP
        opt.Window = TimeSpan.FromMinutes(1);
    });
});




// Register services using your modular service registration pattern
new ServiceRegistration().AddServices(builder.Services, builder.Configuration);
new ServicesRegistration().AddServices(builder.Services);
new RepositoryRegistration().AddServices(builder.Services);
new DatabaseRegistration().AddServices(builder.Services, builder.Configuration);
new UserServiceManager().AddServices(builder.Services);
new AuthorizationServiceRegistration().AddServices(builder.Services);
//builder.Services.AddApplicationInsightsTelemetry();

// verfiy single service registration
var serviceDescriptors = builder.Services.Where(
    s => s.ServiceType == typeof(IHybridCacheService)).ToList();

Console.WriteLine($" HybridCacheService registrations found: {serviceDescriptors.Count}");
foreach (var descriptor in serviceDescriptors)
{
    Console.WriteLine($"   - {descriptor.Lifetime}: {descriptor.ImplementationType?.Name}");
}


var app = builder.Build();



// Auto-migrate database on startup


using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        Console.WriteLine("Attempting to connect to database...");

        // Wait for database to be ready
        var maxRetries = 30;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                if (await db.Database.CanConnectAsync())
                {
                    Console.WriteLine("Database connection successful!");
                    Console.WriteLine("Starting database migration...");
                    await db.Database.MigrateAsync();
                    Console.WriteLine("Database migration completed successfully!");
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database connection attempt {retryCount + 1}/{maxRetries} failed: {ex.Message}");
                retryCount++;

                if (retryCount >= maxRetries)
                {
                    Console.WriteLine("Failed to connect to database after maximum retries.");
                    throw;
                }

                await Task.Delay(2000); // Wait 2 seconds before retry
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database migration failed: {ex.Message}");
        // Don't crash the app, but log the error
    }

    try
    {
        var cacheService = scope.ServiceProvider.GetRequiredService<IHybridCacheService>();
        var health = await cacheService.GetHealthAsync();

        if (health.IsRedisConnected)
        {
            Console.WriteLine(" Redis cloud connection successful");
            Console.WriteLine($" Redis latency: {health.RedisLatency.TotalMilliseconds:F2}ms");
        }
        else
        {
            Console.WriteLine(" Redis connection failed - using memory cache only");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($" Cache service initialization failed: {ex.Message}");
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EcommerceInstant API v1");
    c.RoutePrefix = string.Empty; // Serve at root
});


// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI(c =>
//     {
//         c.SwaggerEndpoint("/swagger/v1/swagger.json", "EcommerceInstant API v1");
//         c.RoutePrefix = string.Empty;
//     });
// }
app.UseStaticFiles();
app.UseHttpsRedirection();

//app.UseCors("AllowAll");
app.UseCors("Development"); // Use most permissive for development


app.UseAuthentication();
app.UseAuthorization();

//  Add specific CORS for payment callbacks
app.Use(async (context, next) =>
{
    //  Special handling for payment callback routes

    if (context.Request.Path.StartsWithSegments("/payment/callback"))
    {
        context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
        context.Response.Headers["Access-Control-Allow-Headers"] = "*";

        if (context.Request.Method == "OPTIONS")
        {
            context.Response.StatusCode = 200;
            return;
        }
    }


    await next();
});

// lightweight middleware for security
app.UseMiddleware<BusinessSecurityMiddleware>();
app.UseRateLimiter();
app.MapCarter();
app.MapHub<AdminNotificationHub>("/adminNotifications");
app.MapHub<UserNotificationHub>("/userNotifications");




app.MapGet("/dbinfo", async (MainDbContext db) =>
{
    try
    {
        bool canConnect = await db.Database.CanConnectAsync();
        var provider = db.Database.ProviderName;
        return $"DB Provider: {provider}, Can Connect: {canConnect}";
    }
    catch (Exception ex)
    {
        return $"DB Error: {ex.Message}";
    }
});

app.MapGet("/health", async (MainDbContext db) =>
{
    try
    {
        bool canConnect = await db.Database.CanConnectAsync();
        return Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            database = canConnect ? "connected" : "disconnected"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Health check failed: {ex.Message}");
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


app.Run();


public partial class Program
{
    // This partial class is required for the WebApplicationFactory to work correctly
    // in integration tests. It allows the factory to create an instance of the Program class.
}