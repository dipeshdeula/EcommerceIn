using Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class PaymentMethodRepository : Repository<PaymentMethod>,IPaymentMethodRepository
    {
        private readonly MainDbContext _context;
        public PaymentMethodRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
