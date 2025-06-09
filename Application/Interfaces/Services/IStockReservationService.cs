using Application.Common;
using Application.Dto;
using Application.Features.CartItemFeat.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public interface IStockReservationService
{
    Task<Result<CartItemDTO>> ReserveStockAsync(CreateCartItemCommand request, string correlationId, string replyTo);
}