using Application.Interfaces.Repositories;
using Domain.Entities.Common;
using Domain.Enums.BannerEventSpecial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class EventRuleRepository : Repository<EventRule>, IEventRuleRepository
    {
        private readonly MainDbContext _context;

        public EventRuleRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<EventRule>> GetByEventIdAsync(int eventId)
        {
            return await _context.EventRules
                .Where(r => r.BannerEventId == eventId && !r.IsDeleted)
                .OrderBy(r => r.Priority)
                .ToListAsync();
        }

        public async Task<IEnumerable<EventRule>> GetActiveRulesByEventIdAsync(int eventId)
        {
            return await _context.EventRules
                .Where(r => r.BannerEventId == eventId && r.IsActive && !r.IsDeleted)
                .OrderBy(r => r.Priority)
                .ToListAsync();
        }

        public async Task<IEnumerable<EventRule>> GetRulesByTypeAsync(RuleType ruleType)
        {
            return await _context.EventRules
                .Where(r => r.Type == ruleType && r.IsActive && !r.IsDeleted)
                .Include(r => r.BannerEvent)
                .Where(r => r.BannerEvent.IsActive && !r.BannerEvent.IsDeleted)
                .ToListAsync();
        }
    }
}
