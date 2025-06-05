using Application.Dto.BannerEventSpecialDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IEventValidationService
    {
        Task<ValidationResult> ValidateEventAsync(AddBannerEventSpecialDTO eventDto);
        Task<ValidationResult> ValidateEventDatesAsync(DateTime startDate, DateTime endDate);
        Task<ValidationResult> ValidateEventConflictsAsync(AddBannerEventSpecialDTO eventDto);
        Task<ValidationResult> ValidateEventRulesAsync(List<AddEventRuleDTO> rules);
        Task<bool> CanActivateEventAsync(int eventId);
    }
}
