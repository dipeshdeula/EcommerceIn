using Application.Interfaces.Repositories;
using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public interface IEventUsageRepository : IRepository<EventUsage>
    {
        Task<IEnumerable<EventUsage>> GetByEventIdAsync(int eventId);
        Task<IEnumerable<EventUsage>> GetByUserIdAsync(int userId);
        Task<int> GetUsageCountByEventAndUserAsync(int eventId, int userId);
        Task<EventUsage?> GetByOrderIdAsync(int orderId);
    }
}
