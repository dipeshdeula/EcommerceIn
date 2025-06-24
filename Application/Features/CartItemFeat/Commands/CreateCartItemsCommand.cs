using Application.Common;
using Application.Dto.CartItemDTOs;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CartItemFeat.Commands
{
    public record CreateCartItemsCommand (
        int UserId,
        List<AddToCartItemDTO> Items
        ) : IRequest<Result<List<CartItemDTO>>>;

    public class CreateCartItemsCommandHandler : IRequestHandler<CreateCartItemsCommand, Result<List<CartItemDTO>>>
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CreateCartItemsCommand> _logger;

        public CreateCartItemsCommandHandler(
            ICartService cartService,
            ILogger<CreateCartItemsCommand> logger
            )
        {
            _cartService = cartService;
            _logger = logger;
        }

        public async Task<Result<List<CartItemDTO>>> Handle(CreateCartItemsCommand request, CancellationToken cancellationToken)
        {
            var results = new List<CartItemDTO>();
            foreach (var item in request.Items)
            {
                var result = await _cartService.AddItemToCartAsync(request.UserId, item);
                if (result.Succeeded && result.Data != null)
                {
                    results.Add(result.Data);
                }
                else
                {
                    _logger.LogWarning("Failed to add item to cart: UserId={UserId}, ProductId={ProductId}, Reason={Reason}",
                        request.UserId, item.ProductId, result.Message);
                }
            }
            return Result<List<CartItemDTO>>.Success(results, "Cart items processed");
        }
    }

}
