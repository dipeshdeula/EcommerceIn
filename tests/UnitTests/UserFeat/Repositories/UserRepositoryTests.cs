using Application.Interfaces.Services;
using AutoFixture;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence.Contexts;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace UnitTests.UserFeat.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly MainDbContext _context;
    private readonly Mock<IFileServices> _mockFileServices;
    private readonly UserRepository _repository;
    private readonly Fixture _fixture;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        _context = new MainDbContext(options);
        _mockFileServices = new Mock<IFileServices>();
        _repository = new UserRepository(_context, _mockFileServices.Object);

        _fixture = new Fixture();
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GetByEmailAsync_ExistingUser_ShouldReturnUser()
    {
        // Arrange
        var user = _fixture.Build<User>()
            .With(u => u.Email, "existing@test.com")
            .With(u => u.IsDeleted, false)
            .Without(u => u.Addresses)
            .Create();

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync("existing@test.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("existing@test.com");
        result.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_NonExistentUser_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexistent@test.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_DeletedUser_ShouldReturnNull()
    {
        // Arrange
        var user = _fixture.Build<User>()
            .With(u => u.Email, "deleted@test.com")
            .With(u => u.IsDeleted, true)
            .Without(u => u.Addresses)
            .Create();

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync("deleted@test.com");

        // Assert
        result.Should().BeNull(); // Should not return deleted users
    }

    [Fact]
    public async Task AddAsync_ValidUser_ShouldAddToDatabase()
    {
        // Arrange
        var user = _fixture.Build<User>()
            .Without(u => u.Id)
            .Without(u => u.Addresses)
            .With(u => u.CreatedAt, DateTime.UtcNow)
            .With(u => u.IsDeleted, false)
            .Create();

        // Act
        var result = await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);

        var savedUser = await _context.Users.FindAsync(result.Id);
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be(user.Email);
        savedUser.Name.Should().Be(user.Name);
        savedUser.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeleteUserAsync_ExistingUser_ShouldMarkAsDeleted()
    {
        // Arrange
        var user = _fixture.Build<User>()
            .With(u => u.IsDeleted, false)
            .Without(u => u.Addresses)
            .Create();

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _repository.SoftDeleteUserAsync(user.Id, CancellationToken.None);

        // Assert
        var deletedUser = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == user.Id);
        deletedUser.Should().NotBeNull();
        deletedUser!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task UndeleteUserAsync_DeletedUser_ShouldRestoreUser()
    {
        // Arrange
        var user = _fixture.Build<User>()
            .With(u => u.IsDeleted, true)
            .Without(u => u.Addresses)
            .Create();

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.UndeleteUserAsync(user.Id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        var restoredUser = await _context.Users.FindAsync(user.Id);
        restoredUser.Should().NotBeNull();
        restoredUser!.IsDeleted.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}