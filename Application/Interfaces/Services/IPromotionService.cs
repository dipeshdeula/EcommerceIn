using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IPromotionService
    {
        Task<List<ApplicablePromotionDTO>> GetApplicablePromotionsAsync(Product product, AppUser user, DateTime currentDate);
        Task<decimal> CalculateDiscountAsync(int eventId, Product product, decimal originalPrice, AppUser user);
        Task<bool> ValidatePromotionUsageAsync(int eventId, int userId);
        Task<bool> CanUserUsePromotionAsync(int eventId, int userId);
        Task<EventEffectivenessDTO> GetEventPerformanceAsync(int eventId);
        Task<bool> IsEventActiveAsync(int eventId, DateTime currentDate);
    }
}
