using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Google.Apis.Auth;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Features.Authentication.Commands
{
    public record VerifyGoogleTokenCommand(string idToken) : IRequest<IResult>;

    public class VerifyGoogleTokenCommandHandler : IRequestHandler<VerifyGoogleTokenCommand, IResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VerifyGoogleTokenCommand> _logger;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public VerifyGoogleTokenCommandHandler(
            IUserRepository userRepository,
            ILogger<VerifyGoogleTokenCommand> logger,
            IConfiguration configuration,
            ITokenService tokenService,
            IRefreshTokenRepository refreshTokenRepository)
        {
            _userRepository = userRepository;
            _logger = logger;
            _configuration = configuration;
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<IResult> Handle(VerifyGoogleTokenCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Verifying Google token...");

                // Validate the Google token
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["Google:ClientId"] }
                });

                if (payload == null)
                {
                    _logger.LogWarning("Invalid Google token.");
                    return Results.BadRequest("Invalid Google token.");
                }

                // Extract user information from the payload
                var email = payload.Email;
                var name = payload.Name;
                var profile = payload.Picture;

                // Check if the user already exists in the database
                var user = await _userRepository.Queryable.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    // If the user does not exist, create a new user
                    user = new User
                    {
                        Name = name,
                        Email = email,
                        ImageUrl = profile,
                        CreatedAt = DateTime.UtcNow,
                    };

                    await _userRepository.AddAsync(user, cancellationToken);
                    await _userRepository.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("New user created successfully.");
                }
                else
                {
                    _logger.LogInformation("Existing user found. Skipping user creation.");
                }

                // Generate access and refresh tokens
                _logger.LogInformation("Generating access and refresh tokens...");
                var accessToken = _tokenService.GenerateAccessToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken(user);
                _logger.LogInformation("Tokens generated successfully.");

                // Create and save the refresh token entity
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    JwtId = Guid.NewGuid().ToString(),
                    CreatedDateTimeUtc = DateTime.UtcNow,
                    ExpiryDateTimeUtc = DateTime.UtcNow.AddDays(7),
                    Used = false,
                    Invalidated = false,
                    UserId = user.Id // Ensure the UserId is set correctly
                };

                await _refreshTokenRepository.AddAsync(refreshTokenEntity);
                _logger.LogInformation("Refresh token saved successfully.");

                // Return the response
                return Results.Ok(new
                {
                    Message = "Google token verified successfully",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    ImageUrl = user.ImageUrl
                });
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning($"Invalid Google token: {ex.Message}");
                return Results.BadRequest("Invalid Google token.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Internal server error: {ex.Message}");
                return Results.Problem("Internal server error");
            }
        }
    }


}




