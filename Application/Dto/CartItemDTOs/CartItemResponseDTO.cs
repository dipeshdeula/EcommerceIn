using Application.Dto.ShippingDTOs;
using Application.Dto.UserDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Dto.CartItemDTOs
{
    public class CartItemResponseDTO
    {
        public int UserId { get; set; }
        public UserDTO? User { get; set; }
        public List<CartItemDTO> Items { get; set; } = new();
        
        // CART TOTALS (calculated once for entire cart)
        public decimal TotalItemPrice { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalEventDiscounts { get; set; }
        public decimal TotalPromoDiscounts { get; set; }

        [JsonIgnore]
        public decimal ShippingCost { get; set; }
        public decimal GrandTotal { get; set; }
        
        // SHIPPING INFO (once per cart)
        public ShippingInfoDTO? Shipping { get; set; }
        [JsonIgnore]
        public string? ShippingMessage { get; set; }
        public bool HasFreeShipping { get; set; }
        
        // CART METADATA
        public int TotalItems => Items.Count;
        public int ActiveItems => Items.Count(i => i.IsActive);
        public int ExpiredItems => Items.Count(i => i.IsExpired);
        public bool CanCheckout => ActiveItems > 0 && ExpiredItems == 0;
        
    
    }
}
