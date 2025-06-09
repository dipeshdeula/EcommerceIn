using Application.Dto;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken(User user);
        string GetServiceToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        Task<TokenResponseDto> RefreshTokenAsync(TokenRequestDto tokenRequest);
    }
}
