/*using Application.Common.Helper;
using Application.Dto;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Settings;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.Authentication.Queries.Login
{
    public record LoginQuery(string Email, string Password) : IRequest<IResult>;

    public class LoginQueryHandler : IRequestHandler<LoginQuery, IResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly JwtTokenSetting _jwtTokenSetting;

        public LoginQueryHandler(IUserRepository userRepository, ITokenService tokenService, IRefreshTokenRepository refreshTokenRepository, JwtTokenSetting jwtTokenSetting)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;
            _jwtTokenSetting = jwtTokenSetting;
        }

        public async Task<IResult> Handle(LoginQuery query, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(query.Email);
            if (user == null)
            {
                return Results.BadRequest(new { Message = "Invalid email or password." });
            }

            if(!PasswordHelper.verifyPassword(query.Password, user.Password))
            {
                return Results.BadRequest(new { Message = "Invalid email or password." });
            }
            

            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken(user);

            // Save the refresh token in the database
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                JwtId = Guid.NewGuid().ToString(),
                CreatedDateTimeUtc = DateTime.UtcNow,
                ExpiryDateTimeUtc = DateTime.UtcNow.AddDays(7),
                UserId = user.Id,
                User = user
            };
            await _refreshTokenRepository.AddAsync(refreshTokenEntity);

            return Results.Ok(new TokenResponseDto
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = (int)_jwtTokenSetting.ExpirationMinutes,
                Message = "Login successful."
            });
        }
    }
}

*/