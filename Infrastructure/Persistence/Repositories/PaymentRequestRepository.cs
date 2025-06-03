using Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class PaymentRequestRepository : Repository<PaymentRequest>,IPaymentRequestRepository
    {
        private readonly MainDbContext _context;
        public PaymentRequestRepository(MainDbContext context) : base(context) 
        {
            _context = context;
        }
    }
}
