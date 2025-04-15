using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken> GetByTokenAsync(string token);
        Task AddAsync(RefreshToken refreshToken);
        Task UpdateAsync(RefreshToken refreshToken);
        Task<bool> ValidateRefreshTokenAsync(int userId, string refreshToken);
        Task UpdateRefreshTokenAsync(int userId, string oldRefreshToken, string newRefreshToken);
    }
}
