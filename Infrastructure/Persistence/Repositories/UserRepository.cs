using Application.Enums;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
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

        public override async Task<User> FindByIdAsync(object id)
        {
            return await _context.Users
                .Include(u => u.Addresses) // Ensure addresses are included
                .FirstOrDefaultAsync(u => u.Id == (int)id);
        }
        public  async Task<IEnumerable<User>> GetAllAsync(
           Expression<Func<User, bool>> predicate = null,
           Func<IQueryable<User>, IOrderedQueryable<User>> orderBy = null,
           int? skip = null,
           int? take = null,
           bool includeDeleted = false,
           string includeProperties = null


           )
        {
            var query = _context.Users.Include(u => u.Addresses).AsQueryable();

            // Include navigation properties dynamically
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }

            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            // Exclude soft-deleted entities if includeDeleted is false
            if (!includeDeleted)
            {
                query = query.Where(u => !u.IsDeleted);
            }

            return await query.ToListAsync();
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



