using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.BannerEventSpecialDTOs
{
    public class EvaluationContextDTO
    {
        public List<CartItem> CartItems { get; set; } = new();
        public User? User { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal? OrderTotal { get; set; }
        public DateTime EvaluationTime { get; set; } = DateTime.UtcNow;
    }
}
