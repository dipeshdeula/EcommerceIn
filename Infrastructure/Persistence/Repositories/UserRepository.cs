using Application.Enums;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        private readonly MainDbContext _context;
        private readonly IFileServices _fileServices;

        public UserRepository(MainDbContext context, IFileServices fileServices) : base(context)
        {
            _context = context;
            _fileServices = fileServices;
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task SoftDeleteUserAsync(int userId, CancellationToken cancellationToken)
        { 
            var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);

            if (user != null)
            {
                //if (!string.IsNullOrEmpty(user.ImageUrl))
                //{
                //    await _fileServices.DeleteFileAsync(user.ImageUrl, FileType.UserImages);
                //}
                await _context.SoftDeleteAsync(user, cancellationToken);
            }
        }

        public async Task HardDeleteUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user != null)
            {
                // Delete related refresh tokens
                var refreshTokens = _context.RefreshTokens.Where(rt => rt.UserId == userId);
                _context.RefreshTokens.RemoveRange(refreshTokens);

                if (!string.IsNullOrEmpty(user.ImageUrl))
                {
                    await _fileServices.DeleteFileAsync(user.ImageUrl, FileType.UserImages);
                }
                await _context.HardDeleteAsync(user, cancellationToken);
            }
        }

        public async Task<bool> UndeleteUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user != null)
            {
                return await _context.UndeleteAsync(user, cancellationToken);
            }
            return false;
        }
    }
}



