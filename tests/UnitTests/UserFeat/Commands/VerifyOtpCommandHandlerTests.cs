using Application.Common;
using Application.Features.Authentication.Otp.Commands;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoFixture;
using Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests.UserFeat.Commands;

public class VerifyOtpCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IOtpService> _mockOtpService; //  Mock interface
    private readonly VerifyOtpCommandHandler _handler;
    private readonly Fixture _fixture;

    public VerifyOtpCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockOtpService = new Mock<IOtpService>(); //  Mock interface

        _handler = new VerifyOtpCommandHandler(
            _mockUserRepository.Object,
            _mockOtpService.Object //  Works now
        );

        _fixture = new Fixture();
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_ValidOtp_ShouldVerifyAndCreateUser()
    {
        // Arrange
        var email = "test@example.com";
        var otp = "123456";
        var command = new VerifyOtpCommand(email, otp);

        var storedUser = new User
        {
            Name = "Test User",
            Email = email,
            Contact = "9812345678",
            CreatedAt = DateTime.UtcNow
        };
        var storedPassword = "hashedPassword";

        _mockOtpService
            .Setup(x => x.ValidateOtp(email, otp))
            .Returns(true); 

        _mockOtpService
            .Setup(x => x.GetUserInfo(email))
            .Returns((storedUser, storedPassword)); 

        _mockUserRepository
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be("OTP verified successfully. Account has been created.");

        _mockOtpService.Verify(x => x.ValidateOtp(email, otp), Times.Once); //  Works!
        _mockOtpService.Verify(x => x.GetUserInfo(email), Times.Once); //  Works!
        _mockUserRepository.Verify(x => x.AddAsync(
            It.Is<User>(u => u.Email == email),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidOtp_ShouldReturnFailure()
    {
        // Arrange
        var email = "test@example.com";
        var otp = "wrong123";
        var command = new VerifyOtpCommand(email, otp);

        _mockOtpService
            .Setup(x => x.ValidateOtp(email, otp))
            .Returns(false); 

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Invalid or expired OTP.");

        _mockOtpService.Verify(x => x.ValidateOtp(email, otp), Times.Once); //  Works!
        _mockOtpService.Verify(x => x.GetUserInfo(It.IsAny<string>()), Times.Never);
        _mockUserRepository.Verify(x => x.AddAsync(
            It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoStoredUserInfo_ShouldReturnFailure()
    {
        // Arrange
        var email = "test@example.com";
        var otp = "123456";
        var command = new VerifyOtpCommand(email, otp);

        _mockOtpService
            .Setup(x => x.ValidateOtp(email, otp))
            .Returns(true);

        _mockOtpService
            .Setup(x => x.GetUserInfo(email))
            .Returns((null, null)); //  No stored user info

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("User information not found.");
    }
}