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
    public record SoftDeleteOrderCommand (int Id) : IRequest<Result<string>>;

    public class SoftDeleteOrderCommandHandler : IRequestHandler<SoftDeleteOrderCommand, Result<string>>
    {
        private readonly IOrderRepository _orderRepository;

        public SoftDeleteOrderCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }
        public async Task<Result<string>> Handle(SoftDeleteOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await _orderRepository.FindByIdAsync(request.Id);
            if (order == null)
            {
                return Result<string>.Failure("Order Id not found");
            }

            await _orderRepository.SoftDeleteAsync(order, cancellationToken);
            await _orderRepository.SaveChangesAsync(cancellationToken);

            return Result<string>.Success(null, "Order soft deleted successfully");
        }
    }

}
