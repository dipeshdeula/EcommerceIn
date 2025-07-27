using Application.Common;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace IntegrationTests.Common
{
    /// <summary>
    /// Test models that mirror your actual application DTOs/Commands/Queries
    /// These are used only for integration testing to match API contracts
    /// </summary>
    
    // ✅ Command/Query models for testing
    public record TestVerifyOtpCommand(string Email, string Otp) : IRequest<Result<string>>;
    public record TestLoginQuery(string Email, string Password) : IRequest<IResult>;
    public record TestVerifyGoogleTokenCommand(string Token) : IRequest<IResult>;
    
    // ✅ DTO models for testing  
    public class TestTokenRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
    
    public class TestForgotPasswordDTO
    {
        public string Email { get; set; } = string.Empty;
    }
    
    public class TestRegisterUserDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
    }

    // ✅ Response models for testing
    public class TestApiResponse<T>
    {
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public bool Succeeded { get; set; }
        public string[]? Errors { get; set; }
    }

    public class TestLoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public object? User { get; set; }
    }
}