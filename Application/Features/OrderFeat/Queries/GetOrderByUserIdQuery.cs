using Application.Common;
using Application.Dto.OrderDTOs;
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
            .Include(uo => uo.User)
            .ThenInclude(uo=>uo.Addresses)
            .Include(uo => uo.Items)
            .OrderByDescending(uo => uo.OrderDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

            var totalCount = await _orderRepository.GetQueryable()
            .Where(uo=>uo.UserId == request.UserId && !uo.IsDeleted).CountAsync(cancellationToken);

            var userOrderDTOs = userOrders.Select(u => u.ToDTO()).ToList();

            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
            return Result<IEnumerable<OrderDTO>>.Success(
                userOrderDTOs,
                "order fetched successfully",
                request.PageNumber,
                request.PageSize,
                totalPages
            );

        }
    }
}
