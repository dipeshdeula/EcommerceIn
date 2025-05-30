using Application.Extension;
using Carter;
using Infrastructure.DependencyInjection;
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
});
builder.Services.AddAuthenticationExtension(builder.Configuration);
builder.Services.AddCarterExtension();
builder.Services.AddCorsExtension();

// Configure JSON serializer to handle reference loops and increase maximum depth
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
    options.JsonSerializerOptions.MaxDepth = 128; // Increase the maximum depth
});

// Register services using your modular service registration pattern
new ServiceRegistration().AddServices(builder.Services, builder.Configuration);
new ServicesRegistration().AddServices(builder.Services);
new RepositoryRegistration().AddServices(builder.Services);
new DatabaseRegistration().AddServices(builder.Services, builder.Configuration);
new UserServiceManager().AddServices(builder.Services);



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

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();
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
