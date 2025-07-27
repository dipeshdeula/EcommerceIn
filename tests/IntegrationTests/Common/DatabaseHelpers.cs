using Bogus;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Security.Cryptography;
using System.Text;

namespace IntegrationTests.Common;

public static class DatabaseHelpers
{
     // Add this helper method
    public static async Task InitializeDatabaseAsync(MainDbContext context)
    {
        try
        {
            Console.WriteLine("Initializing in-memory database...");
            
            // For in-memory database, use EnsureCreated instead of Migrate
            if (context.Database.IsInMemory())
            {
                await context.Database.EnsureCreatedAsync();
                Console.WriteLine("Database created successfully");
            }
            else
            {
                // This branch shouldn't run in tests, but keep it for completeness
                await context.Database.MigrateAsync();
                Console.WriteLine("Database migrated successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization error: {ex.Message}");
            throw;
        }
    }
    
    // Add this extension method to check if using in-memory database
    public static bool IsInMemory(this DatabaseFacade database)
    {
        return database.ProviderName.Contains("InMemory");
    }
    private static readonly Faker<User> UserFaker = new Faker<User>()
        .RuleFor(u => u.Name, f => f.Person.FullName)
        .RuleFor(u => u.Email, f => f.Person.Email)
        .RuleFor(u => u.Contact, f => f.Phone.PhoneNumber("98########"))
        .RuleFor(u => u.Password, f => HashPassword("testpassword"))
        .RuleFor(u => u.Role, f => UserRoles.User)
        .RuleFor(u => u.IsDeleted, false)
        .RuleFor(u => u.CreatedAt, f => f.Date.Recent(30));

    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "test_salt"));
        return Convert.ToBase64String(hashedBytes);
    }

    public static async Task SeedTestDataAsync(MainDbContext context)
    {
        if (context.Users.Any())
            return; // Already seeded

        var users = UserFaker.Generate(5);
        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
    }

    public static async Task<User> CreateTestUserAsync(MainDbContext context, string? email = null)
    {
        var user = UserFaker.Generate();
        if (!string.IsNullOrEmpty(email))
            user.Email = email;

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        
        await context.Entry(user).ReloadAsync();
        return user;
    }

    public static async Task ClearDatabaseAsync(MainDbContext context)
    {
        context.Users.RemoveRange(context.Users);
        await context.SaveChangesAsync();
    }
}