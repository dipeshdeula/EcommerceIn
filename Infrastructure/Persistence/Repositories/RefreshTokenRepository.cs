using Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly MainDbContext _context;

        public RefreshTokenRepository(MainDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(RefreshToken refreshToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async Task<RefreshToken> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task UpdateAsync(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();

        }
      

        public async Task<bool> ValidateRefreshTokenAsync(int userId, string refreshToken)
        {
            var token = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == refreshToken && !rt.Used && !rt.Invalidated);
            return token != null;
        }

        public async Task UpdateRefreshTokenAsync(int userId, string oldRefreshToken, string newRefreshToken)
        { 
            var token = await _context.RefreshTokens.FirstOrDefaultAsync(rt=>rt.Token == oldRefreshToken && rt.UserId == userId);
            if (token != null)
            {
                token.Token = newRefreshToken;
                token.Used = false;
                token.Invalidated = false;
                token.ExpiryDateTimeUtc = DateTime.UtcNow.AddDays(7);
                _context.RefreshTokens.Update(token);
                await _context.SaveChangesAsync();
            }
        }
    }
}
