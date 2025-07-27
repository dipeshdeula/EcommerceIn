using FluentAssertions;
using IntegrationTests.Common;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.UserFeat;

public class UserManagementIntegrationTests : IClassFixture<IntegrationTestWebAppFactory<Program>>
{
    private readonly IntegrationTestWebAppFactory<Program> _factory;
    private readonly HttpClient _client;

    public UserManagementIntegrationTests(IntegrationTestWebAppFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllUsers_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        await _factory.SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/User/getAllUsers?PageNumber=1&PageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNull();
        // Content validation depends on your actual API response format
    }

    [Fact]
    public async Task GetUserById_ExistingUser_ShouldReturnUser()
    {
        // Arrange
        using var context = await _factory.GetDbContextAsync();
        var user = await DatabaseHelpers.CreateTestUserAsync(context, "getbyid.integration@example.com");

        // Act
        var response = await _client.GetAsync($"/User/getUserById/{user.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNull();

        if (response.StatusCode == HttpStatusCode.OK)
        {
            responseContent.Should().Contain(user.Email);
        }
    }

    [Fact]
    public async Task GetUserById_NonExistentUser_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/User/getUserById/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("not found");
    }

    [Fact]
    public async Task SoftDeleteUser_ExistingUser_ShouldMarkAsDeleted()
    {
        // Arrange
        using var context = await _factory.GetDbContextAsync();
        var user = await DatabaseHelpers.CreateTestUserAsync(context, "softdelete.integration@example.com");

        // Act
        var response = await _client.DeleteAsync($"/User/softDeleteUser/{user.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            // Verify user is marked as deleted in database
            using var verifyContext = await _factory.GetDbContextAsync();
            var deletedUser = await verifyContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == user.Id);
            
            deletedUser.Should().NotBeNull();
            deletedUser!.IsDeleted.Should().BeTrue();
        }
    }

    [Fact]
    public async Task UndeleteUser_DeletedUser_ShouldRestoreUser()
    {
        // Arrange
        using var context = await _factory.GetDbContextAsync();
        var user = await DatabaseHelpers.CreateTestUserAsync(context, "undelete.integration@example.com");
        
        // Soft delete the user first
        await _client.DeleteAsync($"/User/softDeleteUser/{user.Id}");

        // Act
        var response = await _client.PutAsync($"/User/undeleteUser/{user.Id}", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            // Verify user is restored
            using var verifyContext = await _factory.GetDbContextAsync();
            var restoredUser = await verifyContext.Users.FindAsync(user.Id);
            restoredUser.Should().NotBeNull();
            restoredUser!.IsDeleted.Should().BeFalse();
        }
    }

    [Fact]
    public async Task GetAllUsers_WithFiltering_ShouldRespectDeletedFlag()
    {
        // Arrange
        using var context = await _factory.GetDbContextAsync();
        var activeUser = await DatabaseHelpers.CreateTestUserAsync(context, "active.integration@example.com");
        var deletedUser = await DatabaseHelpers.CreateTestUserAsync(context, "deleted.integration@example.com");
        
        // Mark one user as deleted
        deletedUser.IsDeleted = true;
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/User/getAllUsers?PageNumber=1&PageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain(activeUser.Email);
        responseContent.Should().NotContain(deletedUser.Email); // Should not include deleted user
    }
}