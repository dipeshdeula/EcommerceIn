using Application.Common;
using Application.Dto.CartItemDTOs;
using Application.Features.CartItemFeat.Commands;

namespace Application.Interfaces.Services;

public interface IStockReservationService
{
    Task<Result<CartItemDTO>> ReserveStockAsync(CreateCartItemCommand request, string correlationId, string replyTo);
}