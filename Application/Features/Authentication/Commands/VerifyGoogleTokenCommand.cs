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
        public VerifyGoogleTokenCommandHandler(IUserRepository userRepository,
            ILogger<VerifyGoogleTokenCommand> logger,
            IConfiguration configuration,
            ITokenService tokenService,
            IRefreshTokenRepository refreshTokenRepository
            )
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
                _logger.LogInformation("Verifying Google token..");
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["Google:ClientId"] }
                });

                // Extract user information from the payload
                var userId = payload.Subject;
                var email = payload.Email;
                var name = payload.Name;
                var profile = payload.Picture;
                // check if the user already exists in the database
                var user = await _userRepository.Queryable.FirstOrDefaultAsync(u => u.Email == email);

                if(user == null)
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
                }

                var accessToken = _tokenService.GenerateAccessToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken(user);

                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    JwtId = Guid.NewGuid().ToString(),
                    CreatedDateTimeUtc = DateTime.UtcNow,
                    ExpiryDateTimeUtc = DateTime.UtcNow.AddDays(7),
                    Used = false,
                    Invalidated = false,
                };

                await _refreshTokenRepository.AddAsync(refreshTokenEntity);

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
            catch (Exception ex)
            {
                // Handle other exceptions
                _logger.LogError($"Internal server error: {ex.Message}");
                return Results.Problem("Internal server error");
            }
        }
    }
           
    }




