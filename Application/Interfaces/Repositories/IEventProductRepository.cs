using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IEventProductRepository : IRepository<EventProduct>
    {
        Task<IEnumerable<EventProduct>> GetByEventIdAsync(int eventId);
        Task<IEnumerable<EventProduct>> GetByProductIdAsync(int productId);
        Task<EventProduct?> GetByEventAndProductIdAsync(int eventId, int productId);
    }
}
