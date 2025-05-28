using Application.Extension;
using Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class AddressRepository : Repository<Address>, IAddressRepository
    {
        private readonly MainDbContext _context;
        public AddressRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task HardDeleteAddressAsync(int Id, CancellationToken cancellationToken)
        {
            var address = await _context.Addresses              
                .FirstOrDefaultAsync(a => a.Id == Id, cancellationToken);

            if (address != null)
            {
                await _context.HardDeleteAsync(address, cancellationToken);
            }
        }
    }
}
