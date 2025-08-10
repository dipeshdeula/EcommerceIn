using Application.Dto.ShippingDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.CartItemDTOs
{
    public record CreateCartItemRequest(
        int ProductId,
        int Quantity,
        ShippingRequestDTO? ShippingRequest = null
    );
}
