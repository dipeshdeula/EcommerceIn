using Application.Common;
using Application.Dto.AuthDTOs.GoogleAuthDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Google.Apis.Auth;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Authentication.Commands
{
    public record GoogleAuthCommand(string IdToken, string ClientType = "web") : IRequest<Result<GoogleAuthResponse>>;

    public class GoogleAuthCommandHandler : IRequestHandler<GoogleAuthCommand, Result<GoogleAuthResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleAuthCommandHandler> _logger;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        // Multiple client IDs for different platforms
        private readonly Dictionary<string, string> _clientIds;

        public GoogleAuthCommandHandler(
            IUserRepository userRepository,
            IConfiguration configuration,
            ILogger<GoogleAuthCommandHandler> logger,
            ITokenService tokenService,
            IRefreshTokenRepository refreshTokenRepository)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _logger = logger;
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;

            // Configure multiple client IDs
            _clientIds = new Dictionary<string, string>
            {
                ["web"] = _configuration["GoogleAuth:WebClientId"] ?? "",
                ["android"] = _configuration["GoogleAuth:AndroidClientId"] ?? "",
                ["ios"] = _configuration["GoogleAuth:iOSClientId"] ?? ""
            };
        }


        public async Task<Result<GoogleAuthResponse>> Handle(GoogleAuthCommand request, CancellationToken cancellationToken)
        {
            try
            {

                _logger.LogInformation("Processing Google Authentication for client type : {ClientType}", request.ClientType);

                // step 1 > validate google token
                var googleUserResult = await ValidateGoogleTokenAsync(request.IdToken, request.ClientType);
                if (!googleUserResult.Succeeded)
                {
                    return Result<GoogleAuthResponse>.Failure(googleUserResult.Message);
                }
                var googleUser = googleUserResult.Data;

                // step 2 > find or create user
                var userResult = await FindOrCreateUserAsync(googleUser, cancellationToken);
                if (!userResult.Succeeded)
                {
                    return Result<GoogleAuthResponse>.Failure(userResult.Message);
                }

                var (user, isNewUser) = userResult.Data;

                // step 3 > Generate tokens
                var tokenResult = await GenerateTokensAsync(user, cancellationToken);
                if (!tokenResult.Succeeded)
                {
                    return Result<GoogleAuthResponse>.Failure(tokenResult.Message);
                }

                var tokens = tokenResult.Data;

                // step 4 > create Response
                var response = new GoogleAuthResponse
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                    User = user.ToDTO(),
                    IsNewUser = isNewUser,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("JwtSettings:ExpirationMinutes", 60)),
                    Message = isNewUser ? "Account created and logged in successfully." : "Logged in successfully."
                };

               _logger.LogInformation("Google authentication successful for user: {Email}", user.Email);             

                return Result<GoogleAuthResponse>.Success(response,response.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Google Authentication");
                return Result<GoogleAuthResponse>.Failure("Authentication failed. Please try again.");
            }
        }

        private async Task<Result<GoogleUserInfo>> ValidateGoogleTokenAsync(string idToken, string clientType)
        {
            try
            {
                if (!_clientIds.ContainsKey(clientType))
                {
                    return Result<GoogleUserInfo>.Failure($"Unsupported client type: {clientType}");
                }

                var clientId = _clientIds[clientType];
                if (string.IsNullOrEmpty(clientId))
                {
                    return Result<GoogleUserInfo>.Failure($"Client ID not configured for {clientType}");
                }

                // Validate Google ID Token
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId },
                    IssuedAtClockTolerance = TimeSpan.FromMinutes(5),
                    ExpirationTimeClockTolerance = TimeSpan.FromMinutes(5)
                });

                var googleUser = new GoogleUserInfo
                {
                    GoogleId = payload.Subject,
                    Email = payload.Email,
                    Name = payload.Name,
                    Picture = payload.Picture,
                    EmailVerified = payload.EmailVerified,
                    GivenName = payload.GivenName,
                    FamilyName = payload.FamilyName
                };

                _logger.LogInformation("Google token validated successfully for: {Email}", payload.Email);
                return Result<GoogleUserInfo>.Success(googleUser, "Token validated");
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning("Invalid Google JWT token: {Error}", ex.Message);
                return Result<GoogleUserInfo>.Failure("Invalid Google authentication token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Google token");
                return Result<GoogleUserInfo>.Failure("Token validation failed");
            }
        }

        private async Task<Result<(User user, bool isNewUser)>> FindOrCreateUserAsync(GoogleUserInfo googleUser, CancellationToken cancellationToken)
        {
            try
            {
                //  Check if user exists by email
                var existingUser = await _userRepository.GetByEmailAsync(googleUser.Email);

                if (existingUser != null)
                {
                    // Update existing user's Google info if needed
                    var updated = false;
                    if (string.IsNullOrEmpty(existingUser.ExternalProviderId))
                    {
                        existingUser.ExternalProvider = "Google";
                        existingUser.ExternalProviderId = googleUser.GoogleId;
                        existingUser.EmailVerified = googleUser.EmailVerified;
                        updated = true;
                    }

                    // Update profile picture if empty
                    if (string.IsNullOrEmpty(existingUser.ImageUrl) && !string.IsNullOrEmpty(googleUser.Picture))
                    {
                        existingUser.ImageUrl = googleUser.Picture;
                        updated = true;
                    }

                    if (updated)
                    {
                        await _userRepository.UpdateAsync(existingUser);
                        await _userRepository.SaveChangesAsync(cancellationToken);
                    }

                    return Result<(User, bool)>.Success((existingUser, false), "Existing user found");
                }

                // Create new user
                var newUser = new User
                {                   
                    Name = googleUser.Name,
                    Email = googleUser.Email,
                    ImageUrl = googleUser.Picture,
                    Role = UserRoles.User, 
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    EmailVerified = googleUser.EmailVerified,
                    ExternalProvider = "Google",
                    ExternalProviderId = googleUser.GoogleId,
                  
                };

                await _userRepository.AddAsync(newUser, cancellationToken);
                await _userRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("New Google user created: {Email}", newUser.Email);
                return Result<(User, bool)>.Success((newUser, true), "New user created");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/finding user");
                return Result<(User, bool)>.Failure("User creation failed");
            }
        }


        private async Task<Result<(string AccessToken, string RefreshToken)>> GenerateTokensAsync(User user, CancellationToken cancellationToken)
        {
            try
            {
                // Generate access token
                var accessToken = _tokenService.GenerateAccessToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken(user);

                //  Save refresh token
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    JwtId = Guid.NewGuid().ToString(),
                    CreatedDateTimeUtc = DateTime.UtcNow,
                    ExpiryDateTimeUtc = DateTime.UtcNow.AddDays(7),
                    Used = false,
                    Invalidated = false,
                    UserId = user.Id
                };

                await _refreshTokenRepository.AddAsync(refreshTokenEntity);

                return Result<(string, string)>.Success((accessToken, refreshToken), "Tokens generated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tokens");
                return Result<(string, string)>.Failure("Token generation failed");
            }
        }
    }


}
