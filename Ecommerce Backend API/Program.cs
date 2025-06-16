using Application.Extension;
using Carter;
using Infrastructure.DependencyInjection;
using Infrastructure.Hubs;
using Infrastructure.Persistence.Contexts;
using Infrastructure.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);



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

// Configure JSON serializer to handle reference loops and increase maximum depth
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
    options.JsonSerializerOptions.MaxDepth = 128; // Increase the maximum depth
});

// Validate Khalti and eSewa configurations
var khaltiKey = builder.Configuration["PaymentGateways:Khalti:SecretKey"];
var khaltiBaseUrl = builder.Configuration["PaymentGateways:Khalti:BaseUrl"];
var esewaMerchantId = builder.Configuration["PaymentGateways:Esewa:MerchantId"];
var esewaBaseUrl = builder.Configuration["PaymentGateways:Esewa:BaseUrl"];
var esewaSecret = builder.Configuration["PaymentGateways:Esewa:SecretKey"];

Console.WriteLine($"Khalti Key: {khaltiKey}");
Console.WriteLine($"Khalti Base URL: {khaltiBaseUrl}");
Console.WriteLine($"eSewa Merchant ID: {esewaMerchantId}");
Console.WriteLine($"eSewa Base URL: {esewaBaseUrl}");
Console.WriteLine($"eSewa Secret Key: {esewaSecret}");

if (string.IsNullOrEmpty(khaltiKey) || string.IsNullOrEmpty(khaltiBaseUrl))
{
    throw new InvalidOperationException("Khalti SecretKey or BaseUrl is not configured in appsettings.json");
}

if (string.IsNullOrEmpty(esewaMerchantId) || string.IsNullOrEmpty(esewaBaseUrl))
{
    throw new InvalidOperationException("eSewa MerchantId or BaseUrl is not configured in appsettings.json");
}

// Optional: Configure named HTTP clients for Khalti and eSewa (if needed)
builder.Services.AddHttpClient("KhaltiClient", client =>
{
    client.BaseAddress = new Uri(khaltiBaseUrl);
    client.DefaultRequestHeaders.Add("Authorization", $"Key {khaltiKey}");
});

builder.Services.AddHttpClient("EsewaClient", client =>
{
    client.BaseAddress = new Uri(esewaBaseUrl);
});



// Register services using your modular service registration pattern
new ServiceRegistration().AddServices(builder.Services, builder.Configuration);
new ServicesRegistration().AddServices(builder.Services);
new RepositoryRegistration().AddServices(builder.Services);
new DatabaseRegistration().AddServices(builder.Services, builder.Configuration);
new UserServiceManager().AddServices(builder.Services);
new AuthorizationServiceRegistration().AddServices(builder.Services);


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

// ✅ Add specific CORS for payment callbacks
app.Use(async (context, next) =>
{
    // ✅ Special handling for payment callback routes
    if (context.Request.Path.StartsWithSegments("/payment/callback"))
    {
        context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        context.Response.Headers.Add("Access-Control-Allow-Headers", "*");

        if (context.Request.Method == "OPTIONS")
        {
            context.Response.StatusCode = 200;
            return;
        }
    }

    await next();
});

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

app.MapGet("/health", async (MainDbContext db) => {
    try
    {
        bool canConnect = await db.Database.CanConnectAsync();
        return Results.Ok(new { 
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


app.Run();
