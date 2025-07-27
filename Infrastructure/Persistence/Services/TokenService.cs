using Application.Dto;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Settings;
using Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtTokenSetting _jwtSettings;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserRepository _userRepository;
        public TokenService(IOptions<JwtTokenSetting> jwtSettings,IRefreshTokenRepository refreshTokenRepository, IUserRepository userRepository)
        {
            _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
            _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public string GenerateAccessToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email,user.Email),
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(ClaimTypes.Name,user.Name),
                new Claim(ClaimTypes.Role,user.Role.ToString()),
                new Claim(ClaimTypes.Actor,user.ImageUrl.ToString())
            };

            return GenerateTokenWithExpiry(claims, TimeSpan.FromMinutes(_jwtSettings.ExpirationMinutes));
        }

        

        public string GenerateRefreshToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email,user.Email),
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                new Claim("tokenType","refresh") // custom claim to identify the token type
                    
            };
            return GenerateTokenWithExpiry(claims, TimeSpan.FromDays(7)); // refresh token expires in 7 Days
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException(nameof(token),"The token cannot be null or empty");

            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false, // we want to validate the token even if it's expired
                ClockSkew = TimeSpan.Zero // remove the default clock skew of 5 minutes

            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            var jwtToken = validatedToken as JwtSecurityToken;

            // check if the token is a refresh token
            if(jwtToken == null || !jwtToken.Claims.Any(c=>c.Type == "tokenType" && c.Value == "refresh"))
            { 
                throw new SecurityTokenException("Invalid refresh token");

            }
            return principal; // return the principal if the token is valid

        }

        public async Task<TokenResponseDto> RefreshTokenAsync(TokenRequestDto tokenRequest)
        {
            var principal = GetPrincipalFromExpiredToken(tokenRequest.RefreshToken);
            var email = principal.FindFirstValue(ClaimTypes.Email);

            var user = await _userRepository.GetByEmailAsync(email);

            if(user==null || !await _refreshTokenRepository.ValidateRefreshTokenAsync(user.Id, tokenRequest.RefreshToken))
            {
                return new TokenResponseDto
                {
                    Success = false,
                    Message = "Invalid refresh token"
                };
            }

            var newAccessToken = GenerateAccessToken(user);
            var newRefreshToken = GenerateRefreshToken(user);

            await _refreshTokenRepository.UpdateRefreshTokenAsync(user.Id, tokenRequest.RefreshToken, newRefreshToken);

            return new TokenResponseDto
            {
                Success = true,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = (int)_jwtSettings.ExpirationMinutes,
            };

        }

        // Helper method to generate a token with a specified expiry time

        private string GenerateTokenWithExpiry(List<Claim> claims, TimeSpan expiryDuration)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key ?? throw new ArgumentNullException(nameof(_jwtSettings.Key))));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.
                HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.Add(expiryDuration),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = credentials
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);  
            return tokenHandler.WriteToken(token);
        }
    }   

    public class ServiceTokenService : IServiceTokenService
    {
        private readonly JwtTokenSetting _jwtSettings;

        public ServiceTokenService(IOptions<JwtTokenSetting> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        }

        private string _cachedToken;

        private DateTime _tokenExpiryTime;
        public string GetServiceToken()
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiryTime)
            {
                return _cachedToken;
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {

                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iss, _jwtSettings.Issuer),
                new Claim(JwtRegisteredClaimNames.Aud, _jwtSettings.Audience)
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                signingCredentials: creds
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            _cachedToken = tokenHandler.WriteToken(token);
            _tokenExpiryTime = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

            return _cachedToken;
        }
    }
}
