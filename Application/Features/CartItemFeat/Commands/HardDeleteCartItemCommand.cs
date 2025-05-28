using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CartItemFeat.Commands
{
    public record HardDeleteCartItemCommand (int Id) : IRequest<Result<CartItemDTO>>;

    public class HardDeleteCartItemCommandHandler : IRequestHandler<HardDeleteCartItemCommand, Result<CartItemDTO>>
    {
        private readonly ICartItemRepository _cartItemRepository;
        private readonly IUserRepository _userRepository;
        public HardDeleteCartItemCommandHandler(ICartItemRepository cartItemRepository, IUserRepository userRepository)
        {
            _cartItemRepository = cartItemRepository;
            _userRepository = userRepository;
        }

        public async Task<Result<CartItemDTO>> Handle(HardDeleteCartItemCommand request, CancellationToken cancellationToken)
        {
           var user = await _userRepository.FindByIdAsync(request.Id);
            if (user == null)
                return Result<CartItemDTO>.Failure($"cart item with Id: {request.Id} is not found");

            await _cartItemRepository.DeleteByUserIdAsync(request.Id);

            return Result<CartItemDTO>.Success(null, $"cart item with id {request.Id} is not found");

        }
    }
}
