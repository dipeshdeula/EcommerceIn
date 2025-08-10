using Application.Common;
using Application.Dto.AuthDTOs;
using Application.Features.Authentication.Commands;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoFixture;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace UnitTests.UserFeat.Commands;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IFileServices> _mockFileServices;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IOtpService> _mockOtpService; //  Mock interface
    private readonly RegisterCommandHandler _handler;
    private readonly Fixture _fixture;

    public RegisterCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockFileServices = new Mock<IFileServices>();
        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockOtpService = new Mock<IOtpService>(); //  Mock interface

        _handler = new RegisterCommandHandler(
            _mockUserRepository.Object,
            _mockFileServices.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockOtpService.Object //  This will work now
        );

        _fixture = new Fixture();
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_NewUserRegistration_ShouldGenerateOtpAndSendEmail()
    {
        // Arrange
        var registerDto = new RegisterUserDTO
        {
            Name = "John Doe",
            Email = "john@example.com",
            Password = "SecurePass123!",
            Contact = "9876543210"
        };

        var command = new RegisterCommand(registerDto);
        var generatedOtp = "123456";

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(registerDto.Email))
            .ReturnsAsync((User?)null);

        _mockOtpService
            .Setup(x => x.GenerateOtp(registerDto.Email))
            .Returns(generatedOtp); //  This works now!

        _mockEmailService
            .Setup(x => x.SendEmailAsync(
                registerDto.Email,
                "Account Verification",
                It.Is<string>(content => content.Contains(generatedOtp))))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be("OTP has been sent to your email. Please verify your email with the OTP sent to you");

        // Verify interactions
        _mockUserRepository.Verify(x => x.GetByEmailAsync(registerDto.Email), Times.Once);
        _mockOtpService.Verify(x => x.GenerateOtp(registerDto.Email), Times.Once); //  Works!
        _mockEmailService.Verify(x => x.SendEmailAsync(
            registerDto.Email,
            "Account Verification",
            It.Is<string>(content => content.Contains(generatedOtp))), Times.Once);
        _mockOtpService.Verify(x => x.StoreUserInfo(
            registerDto.Email,
            It.Is<User>(u =>
                u.Name == registerDto.Name &&
                u.Email == registerDto.Email &&
                u.Contact == registerDto.Contact),
            registerDto.Password), Times.Once); //  Works!
    }

    [Fact]
    public async Task Handle_ExistingUser_ShouldReturnFailure()
    {
        // Arrange
        var registerDto = new RegisterUserDTO
        {
            Name = "Jane Doe",
            Email = "existing@example.com",
            Password = "Password123!",
            Contact = "9876543210"
        };

        var command = new RegisterCommand(registerDto);
        var existingUser = _fixture.Build<User>()
            .Without(u => u.Addresses)
            .Create();

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(registerDto.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("User already exists");

        // Verify no OTP operations for existing users
        _mockOtpService.Verify(x => x.GenerateOtp(It.IsAny<string>()), Times.Never); //  Works!
        _mockEmailService.Verify(x => x.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailServiceFailure_ShouldReturnFailure()
    {
        // Arrange
        var registerDto = new RegisterUserDTO
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "Password123!",
            Contact = "9876543210"
        };

        var command = new RegisterCommand(registerDto);
        var generatedOtp = "123456";

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(registerDto.Email))
            .ReturnsAsync((User?)null);

        _mockOtpService
            .Setup(x => x.GenerateOtp(registerDto.Email))
            .Returns(generatedOtp); //  Works!

        _mockEmailService
            .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP server unavailable"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("Invalid OTP");
        result.Message.Should().Contain("SMTP server unavailable");
    }
}