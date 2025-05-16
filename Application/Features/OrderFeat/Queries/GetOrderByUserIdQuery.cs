using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.OrderFeat.Queries
{
    public record GetOrderByUserIdQuery
        (int UserId, int PageNumber, int PageSize) : IRequest<Result<IEnumerable<OrderDTO>>>;

    public class GetOrderByUserIdQueryHandler : IRequestHandler<GetOrderByUserIdQuery, Result<IEnumerable<OrderDTO>>>
    {
        private readonly IOrderRepository _orderRepository;
        public GetOrderByUserIdQueryHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }
        public async Task<Result<IEnumerable<OrderDTO>>> Handle(GetOrderByUserIdQuery request, CancellationToken cancellationToken)
        {
            // Fetch orders and there associated user

            var userOrders = await _orderRepository.GetQueryable()
                            .Where(uo => uo.UserId == request.UserId && !uo.IsDeleted)                         
                            .Include(uo => uo.Items).ToListAsync(cancellationToken);

            var userOrderDTOs = userOrders.Select(u => u.ToDTO()).ToList();
            return Result<IEnumerable<OrderDTO>>.Success(userOrderDTOs, "order fetched successfully");

        }
    }
}
