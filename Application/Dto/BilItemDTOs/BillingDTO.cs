using Application.Dto.CompanyDTOs;
using Application.Dto.OrderDTOs;
using Application.Dto.PaymentDTOs;
using Application.Dto.UserDTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.BilItemDTOs
{
    public class BillingDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public int CompanyInfoId { get; set; }
        public DateTime BillingDate { get; set; } = DateTime.UtcNow;

        public UserDTO? User { get; set; }
        public PaymentRequestDTO? PaymentRequest { get; set; }
        public OrderDTO? Order { get; set; }
        public CompanyInfoDTO? CompanyInfo { get; set; }
        public ICollection<BillingItemDTO> Items { get; set; } = new List<BillingItemDTO>();
    }
}
