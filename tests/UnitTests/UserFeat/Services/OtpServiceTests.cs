using Application.Utilities;
using Domain.Entities;
using Domain.Settings;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace UnitTests.UserFeat.Services;

public class OtpServiceTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<OtpSettings> _mockOtpSettings;
    private readonly OtpService _otpService;

    public OtpServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockConfiguration = new Mock<IConfiguration>();
        _mockOtpSettings = new Mock<OtpSettings>();

        // Setup default configuration values
        _mockConfiguration.Setup(x => x["OtpSettings:ExpirationMinutes"]).Returns("5");
        
        _otpService = new OtpService(_memoryCache, _mockConfiguration.Object, _mockOtpSettings.Object);
    }

    [Fact]
    public void GenerateOtp_ShouldReturn6DigitCode()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var otp = _otpService.GenerateOtp(email);

        // Assert
        otp.Should().NotBeNullOrEmpty();
        otp.Should().HaveLength(6);
        otp.Should().MatchRegex("^[0-9]{6}$");
    }

    [Fact]
    public void GenerateOtp_MultipleCalls_ShouldGenerateDifferentCodes()
    {
        // Arrange
        var email1 = "test1@example.com";
        var email2 = "test2@example.com";

        // Act
        var otp1 = _otpService.GenerateOtp(email1);
        var otp2 = _otpService.GenerateOtp(email2);

        // Assert
        otp1.Should().NotBe(otp2); // Very high probability they're different
    }

    [Fact]
    public void ValidateOtp_ValidCode_ShouldReturnTrue()
    {
        // Arrange
        var email = "test@example.com";
        var generatedOtp = _otpService.GenerateOtp(email);

        // Act
        var isValid = _otpService.ValidateOtp(email, generatedOtp);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateOtp_WrongCode_ShouldReturnFalse()
    {
        // Arrange
        var email = "test@example.com";
        _otpService.GenerateOtp(email);

        // Act
        var isValid = _otpService.ValidateOtp(email, "wrong123");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateOtp_NonExistentEmail_ShouldReturnFalse()
    {
        // Act
        var isValid = _otpService.ValidateOtp("nonexistent@example.com", "123456");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void StoreUserInfo_ShouldStoreAndRetrieveUserData()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User
        {
            Name = "Test User",
            Email = email,
            Contact = "9876543210",
            CreatedAt = DateTime.UtcNow
        };
        var password = "password123";

        // Act
        _otpService.StoreUserInfo(email, user, password);
        var (storedUser, storedPassword) = _otpService.GetUserInfo(email);

        // Assert
        storedUser.Should().NotBeNull();
        storedUser.Name.Should().Be(user.Name);
        storedUser.Email.Should().Be(user.Email);
        storedUser.Contact.Should().Be(user.Contact);
        storedPassword.Should().Be(password);
    }

    [Fact]
    public void GetUserInfo_NonExistentEmail_ShouldReturnNull()
    {
        // Act
        var (storedUser, storedPassword) = _otpService.GetUserInfo("nonexistent@example.com");

        // Assert
        storedUser.Should().BeNull();
        storedPassword.Should().BeNull();
    }

    [Fact]
    public void StoreUserInfo_OverwriteExisting_ShouldUpdateUserData()
    {
        // Arrange
        var email = "test@example.com";
        var user1 = new User { Name = "First User", Email = email };
        var user2 = new User { Name = "Second User", Email = email };

        // Act
        _otpService.StoreUserInfo(email, user1, "password1");
        _otpService.StoreUserInfo(email, user2, "password2");
        var (storedUser, storedPassword) = _otpService.GetUserInfo(email);

        // Assert
        storedUser.Should().NotBeNull();
        storedUser.Name.Should().Be("Second User");
        storedPassword.Should().Be("password2");
    }

    [Fact]
    public void GenerateOtp_SameEmail_ShouldOverwritePreviousOtp()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var otp1 = _otpService.GenerateOtp(email);
        var otp2 = _otpService.GenerateOtp(email);

        // Assert
        otp1.Should().NotBe(otp2);
        
        // First OTP should be invalid, second should be valid
        _otpService.ValidateOtp(email, otp1).Should().BeFalse();
        _otpService.ValidateOtp(email, otp2).Should().BeTrue();
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
    }
}