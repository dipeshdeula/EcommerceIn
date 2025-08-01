using Application.Dto.BannerEventSpecialDTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IBannerEventRuleEngine
    {
        Task<RuleEvaluationResultDTO> EvaluateAllRulesAsync(BannerEventSpecial bannerEvent, EvaluationContextDTO context);
        Task<bool> ValidateCartRulesAsync(int eventId, List<CartItem> cartItems, User user, string? paymentMethod = null);
    }
}
