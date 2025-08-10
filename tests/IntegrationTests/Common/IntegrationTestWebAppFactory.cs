using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Persistence.Contexts;
using Application.Interfaces.Services;
using Infrastructure.DependencyInjection;
using Application.Extension;
using Moq;
using Microsoft.AspNetCore.Http;
using Application.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;

namespace IntegrationTests.Common;

public class IntegrationTestWebAppFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        //  CRITICAL: Set test environment FIRST
        builder.UseEnvironment("Test");
        
        //  Configure AppSettings
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.Sources.Clear();

            //  Use base path to locate the file correctly
            var projectDir = Directory.GetCurrentDirectory();
            var configPath = Path.Combine(projectDir, "appsettings.Test.json");
            
            //  FIXED: Use the absolute path variable instead of relative path
            config.AddJsonFile(configPath, optional: true, reloadOnChange: true);
            
            //  Always have in-memory configuration as fallback
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"ConnectionStrings:DefaultConnection", "InMemory"},
                {"JwtSettings:Key", "TESTKEY_AQUCIKBROWNFOXJUMPSOVERALAZYDOG=@34TESTKEY_AQUCIKBROWNFOXJUMPSOVERALAZYDOG=@34"},
                {"JwtSettings:Issuer", "EcommerceTest"},
                {"JwtSettings:Audience", "EcommerceTest"},
                {"JwtSettings:ExpirationMinutes", "60"},
                {"PaymentGateways:Khalti:SecretKey", "test_khalti_key_123456789"},
                {"PaymentGateways:Khalti:BaseUrl", "https://test.khalti.com/api/v2/"},
                {"PaymentGateways:Esewa:MerchantId", "TEST_MERCHANT_ID"},
                {"PaymentGateways:Esewa:BaseUrl", "https://rc-epay.esewa.com.np"},
                {"PaymentGateways:Esewa:SecretKey", "test_secret_key_123456789"},
                {"OtpSettings:ExpirationMinutes", "5"},
                {"FileSettings:BaseUrl", "http://localhost:5013"},
                {"FileSettings:Root", "wwwroot"},
                {"FileSettings:FileLocation", "uploads"},
                {"EmailSettings:SmtpHost", "localhost"},
                {"EmailSettings:SmtpPort", "1025"},
                {"EmailSettings:FromEmail", "test@example.com"},
                {"EmailSettings:FromPassword", "testpassword"},
                {"EmailSettings:FromName", "Test System"},
                {"EmailSettings:EnableSsl", "false"},
                {"EmailSettings:Timeout", "5000"}
            });
        });

        // Configure services
        builder.ConfigureServices(services =>
        {
            //  Configure test database FIRST
            ConfigureTestDatabase(services);
            
            //  Add controllers explicitly to ensure they're registered
            services.AddControllers()
                    .AddApplicationPart(typeof(TProgram).Assembly);
            
            //  Add MVC and ensure endpoint routing is enabled
            services.AddMvc();
            services.AddRouting();
            
            // Get configuration from the builder
            var configuration = BuildTestConfiguration();
            
            // Register services
            new ServiceRegistration().AddServices(services, configuration);
            new ServicesRegistration().AddServices(services);
            new RepositoryRegistration().AddServices(services);
            new UserServiceManager().AddServices(services);
            new AuthorizationServiceRegistration().AddServices(services);
            
            // Replace services for testing
            ReplaceServicesForTesting(services);
            
            //  Add the DatabaseInitializer for proper test database setup
            services.AddScoped<DatabaseInitializer>();
        });
        
        //  Configure the HTTP request pipeline
       // Inside the Configure method:
builder.Configure(app =>
{
    // Add detailed request logging middleware first
    app.UseMiddleware<RequestDebugMiddleware>();
    
    // Log all registered endpoints
    app.Use(async (context, next) =>
    {
        var endpointFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IEndpointFeature>();
        var endpoint = endpointFeature?.Endpoint;
        
        if (endpoint != null)
        {
            var routePattern = (endpoint as Microsoft.AspNetCore.Routing.RouteEndpoint)?.RoutePattern?.RawText;
            Console.WriteLine($"Matched endpoint: {endpoint.DisplayName}, Route: {routePattern}");
        }
        else
        {
            Console.WriteLine($"No endpoint matched for {context.Request.Method} {context.Request.Path}");
        }
        
        await next();
    });
    
    // Rest of your middleware configuration
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();

        // Log all registered routes for debugging
        var routeEndpoints = endpoints.DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>();

        foreach (var endpoint in routeEndpoints)
        {
            Console.WriteLine($"Registered route: {endpoint.RoutePattern.RawText} -> {endpoint.DisplayName}");
        }
        
    });
});
    }
    
    private static IConfiguration BuildTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"ConnectionStrings:DefaultConnection", "InMemory"},
                {"JwtSettings:Key", "TESTKEY_AQUCIKBROWNFOXJUMPSOVERALAZYDOG=@34TESTKEY_AQUCIKBROWNFOXJUMPSOVERALAZYDOG=@34"},
                {"JwtSettings:Issuer", "EcommerceTest"},
                {"JwtSettings:Audience", "EcommerceTest"},
                {"JwtSettings:ExpirationMinutes", "60"}
            })
            .Build();
    }

    private static void ConfigureTestDatabase(IServiceCollection services)
    {
        //  Remove existing DbContext registration
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MainDbContext>));
        if (descriptor != null)
            services.Remove(descriptor);

        //  Remove other DB-related services that might conflict
        var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(MainDbContext));
        if (dbContextDescriptor != null)
            services.Remove(dbContextDescriptor);

        //  Add InMemory database with proper configuration
        services.AddDbContext<MainDbContext>(options =>
        {
            options.UseInMemoryDatabase($"IntegrationTestDb_{Guid.NewGuid()}");
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            //  Configure for testing
            options.ConfigureWarnings(warnings => {
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning);
                // Add this to ignore migration-related warnings
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning);
            });
        });
    }

    private static void ReplaceServicesForTesting(IServiceCollection services)
    {
        ReplaceService<IEmailService>(services, () =>
        {
            var mock = new Mock<IEmailService>();
            mock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            return mock.Object;
        });

        //  Mock FileServices
        ReplaceService<IFileServices>(services, () =>
        {
            var mock = new Mock<IFileServices>();
            mock.Setup(x => x.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<FileType>()))
                .ReturnsAsync("http://test.com/test-image.jpg");
            return mock.Object;
        });

        //  Mock CurrentUserService
        ReplaceService<ICurrentUserService>(services, () =>
        {
            var mock = new Mock<ICurrentUserService>();
            mock.Setup(x => x.UserId).Returns("test-user-id");
            mock.Setup(x => x.IsAuthenticated).Returns(true);
            mock.Setup(x => x.Role).Returns("User");
            return mock.Object;
        });
        
        // Mock Khalti and eSewa services (if you have concrete implementations)
        foreach (var descriptor in services.Where(s => s.ServiceType.Name.Contains("Khalti") || 
                                                    s.ServiceType.Name.Contains("Esewa")).ToList())
        {
            services.Remove(descriptor);
            
            // Create a mock implementation
            var mockService = new Mock<object>().Object;
            services.AddSingleton(descriptor.ServiceType, mockService);
        }

        //  Add test logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug); // Change to Debug for more info
        });
    }
    
    //  Fixed implementation of ReplaceService
    private static void ReplaceService<T>(IServiceCollection services, Func<T> factory) where T : class
    {
        //  Debug output to identify service type
        Console.WriteLine($"Replacing service: {typeof(T).Name}");
        
        //  Remove ALL existing registrations of this type
        var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
        
        //  Debug output to see how many instances were found
        Console.WriteLine($"Found {descriptors.Count} existing registrations of {typeof(T).Name}");
        
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
        
        // Add the new implementation
        services.AddSingleton<T>(sp => factory());
        Console.WriteLine($"Added new implementation of {typeof(T).Name}");
    }

    /// <summary>
    /// Get database context for test data manipulation
    /// </summary>
    public async Task<MainDbContext> GetDbContextAsync()
    {
        var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        
        //  Use EnsureCreated instead of running migrations
        await context.Database.EnsureCreatedAsync();
        
        return context;
    }

    /// <summary>
    /// Seed test data into database
    /// </summary>
    public async Task SeedTestDataAsync()
    {
        using var scope = Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync();
    }

    /// <summary>
    /// Create HTTP client for testing
    /// </summary>
    public HttpClient CreateTestClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
}

/// <summary>
/// Database initializer to properly set up test data without migrations
/// </summary>
public class DatabaseInitializer
{
    private readonly MainDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(MainDbContext context, ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // For in-memory database, we just need to ensure the database exists
            // No migrations are needed
            await _context.Database.EnsureCreatedAsync();
            
            // Add test users if they don't exist
            if (!_context.Users.Any())
            {
                _logger.LogInformation("Seeding test user data");
                
                // Add test users
                _context.Users.Add(new User 
                { 
                    Id = 1,
                    Name = "Test User", 
                    Email = "test@example.com",
                    Password = DatabaseHelpers.HashPassword("testpassword"),
                    Role = UserRoles.Admin,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully seeded test user data");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing test database");
        }
    }
}

/// <summary>
/// Middleware to log HTTP requests for debugging
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation($"Request: {context.Request.Method} {context.Request.Path}");
        
        try
        {
            await _next(context);
        }
        finally
        {
            _logger.LogInformation($"Response: {context.Response.StatusCode}");
        }
    }
}