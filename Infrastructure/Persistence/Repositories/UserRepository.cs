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

        public UserRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}



