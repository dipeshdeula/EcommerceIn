using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.OrderFeat.DeleteCommands
{
    public record HardDeleteOrderCommand(int Id) : IRequest<Result<string>>;

    public class HardDeleteOrderCommandHandler : IRequestHandler<HardDeleteOrderCommand, Result<string>>
    {
        private readonly IOrderRepository _orderRepository;
        public HardDeleteOrderCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }
        public async Task<Result<string>> Handle(HardDeleteOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await _orderRepository.FindByIdAsync(request.Id);
            if (order == null)
            {
                return Result<string>.Failure("Order Id not found");
            }

            await _orderRepository.RemoveAsync(order, cancellationToken);
            await _orderRepository.SaveChangesAsync(cancellationToken);

            return Result<string>.Success(null, "order hard deleted successfull");
        }
    }

}
