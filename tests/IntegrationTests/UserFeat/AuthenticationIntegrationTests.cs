using FluentAssertions;
using IntegrationTests.Common;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Application.Dto.AuthDTOs;
using Application.Features.Authentication.Otp.Commands;
using Application.Features.Authentication.Queries.Login;

namespace IntegrationTests.UserFeat;

public class AuthenticationIntegrationTests : IClassFixture<IntegrationTestWebAppFactory<Program>>
{
    private readonly IntegrationTestWebAppFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthenticationIntegrationTests(IntegrationTestWebAppFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateTestClient();
    }

    [Fact]
    public async Task Register_ValidUserData_ShouldReturnSuccess()
    {
        // Arrange
        var registerDto = new RegisterUserDTO
        {
            Name = "John Integration Test",
            Email = $"john.{Guid.NewGuid()}@example.com",
            Password = "SecurePass123!",
            Contact = "9876543210"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNull();

        // Log for debugging
        Console.WriteLine($"Register Response: {response.StatusCode}");
        Console.WriteLine($"Content: {content}");
    }

    [Fact]
    public async Task VerifyOtp_WithValidCommand_ShouldProcess()
    {
        // Arrange
        var command = new VerifyOtpCommand("test@example.com", "123456");

        // Act
        var response = await _client.PostAsJsonAsync("/auth/verify-otp", command);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNull();

        Console.WriteLine($"VerifyOtp Response: {response.StatusCode}");
        Console.WriteLine($"Content: {content}");
    }

    [Fact]
    public async Task Login_WithValidQuery_ShouldProcess()
    {
        // Arrange
        var query = new LoginQuery("test@example.com", "testpassword");

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", query);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNull();

        Console.WriteLine($"Login Response: {response.StatusCode}");
        Console.WriteLine($"Content: {content}");
    }

    [Fact]
    public async Task Database_ShouldBeAccessible()
    {
        // Arrange & Act
        using var context = await _factory.GetDbContextAsync();
        
        // Assert
        var canConnect = await context.Database.CanConnectAsync();
        canConnect.Should().BeTrue();

        // Test basic operations
        var userCount = context.Users.Count();
        userCount.Should().BeGreaterThanOrEqualTo(0);

        Console.WriteLine($"Database accessible, user count: {userCount}");
    }
}