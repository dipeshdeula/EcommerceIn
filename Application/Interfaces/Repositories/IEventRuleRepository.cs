using Domain.Entities.Common;
using Domain.Enums.BannerEventSpecial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IEventRuleRepository : IRepository<EventRule>
    {
        Task<IEnumerable<EventRule>> GetByEventIdAsync(int eventId);
        Task<IEnumerable<EventRule>> GetActiveRulesByEventIdAsync(int eventId);
        Task<IEnumerable<EventRule>> GetRulesByTypeAsync(RuleType ruleType);
    }
}
