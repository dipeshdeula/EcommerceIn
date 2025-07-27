using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace IntegrationTests.Common;

/// <summary>
/// Test authentication handler for integration tests
/// Allows bypassing real authentication for testing purposes
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string DefaultUserId = "test-user-123";
    public const string DefaultUserEmail = "test@example.com";
    public const string DefaultUserName = "Test User";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) // ✅ Fixed: Removed ISystemClock (deprecated in .NET 8)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // ✅ Check if authentication should be bypassed
        if (Context.Request.Headers.ContainsKey("X-Skip-Auth"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // ✅ Create test claims with realistic data
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, DefaultUserId),
            new Claim(ClaimTypes.Name, DefaultUserName),
            new Claim(ClaimTypes.Email, DefaultUserEmail),
            new Claim(ClaimTypes.Role, "User"),
            new Claim("sub", DefaultUserId), // JWT standard claim
            new Claim("email", DefaultUserEmail),
            new Claim("name", DefaultUserName),
            new Claim("role", "User")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Extensions for configuring test authentication
/// </summary>
public static class TestAuthExtensions
{
    /// <summary>
    /// Add test authentication scheme for integration tests
    /// </summary>
    public static IServiceCollection AddTestAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, options => { });

        return services;
    }

    /// <summary>
    /// Add test authentication with custom user claims
    /// </summary>
    public static IServiceCollection AddTestAuthentication(this IServiceCollection services, 
        string userId, string email, string name, string role = "User")
    {
        services.AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, options => 
                {
                    // You could configure custom options here if needed
                });

        return services;
    }
}

/// <summary>
/// Helper for creating authenticated HTTP clients in tests
/// </summary>
public static class TestAuthHelpers
{
    /// <summary>
    /// Add authentication header to HTTP client
    /// </summary>
    public static void AddTestAuth(this HttpClient client, string userId = TestAuthHandler.DefaultUserId)
    {
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer test-token-{userId}");
    }

    /// <summary>
    /// Skip authentication for specific request
    /// </summary>
    public static void SkipAuth(this HttpClient client)
    {
        client.DefaultRequestHeaders.Add("X-Skip-Auth", "true");
    }

    /// <summary>
    /// Create test user claims
    /// </summary>
    public static ClaimsPrincipal CreateTestUser(string userId = TestAuthHandler.DefaultUserId, 
        string email = TestAuthHandler.DefaultUserEmail, 
        string name = TestAuthHandler.DefaultUserName,
        string role = "User")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, TestAuthHandler.SchemeName);
        return new ClaimsPrincipal(identity);
    }
}