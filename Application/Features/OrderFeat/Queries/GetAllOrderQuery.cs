using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Application.Extension;
using Application.Dto.OrderDTOs;


namespace Application.Features.OrderFeat.Queries
{
    public record GetAllOrderQuery(int PageNumber,int PageSize) : IRequest<Result<IEnumerable<OrderDTO>>>;

    public class GetAllOrderQueryHandler : IRequestHandler<GetAllOrderQuery, Result<IEnumerable<OrderDTO>>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<GetAllOrderQuery> _logger;
        public GetAllOrderQueryHandler(IOrderRepository orderRepository,ILogger<GetAllOrderQuery> logger)
        {
            _orderRepository = orderRepository;     
            _logger = logger;
        }

        public async Task<Result<IEnumerable<OrderDTO>>> Handle(GetAllOrderQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching all order items with pagination");
                      

             var orders = await _orderRepository.GetAllAsync(
                orderBy:query=>query.OrderByDescending(order=>order.OrderDate),
                skip:(request.PageNumber-1)*request.PageSize,
                take:request.PageSize,               
                includeProperties: "User,User.Addresses, Items",
                cancellationToken:cancellationToken
                );

            // Get total count for pagination
            var totalCount = await _orderRepository.CountAsync(o => !o.IsDeleted, cancellationToken);
            // Map order to DTOs
            var orderDTOs = orders.Select(o => o.ToDTO()).ToList();

            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
            return Result<IEnumerable<OrderDTO>>.Success(
               
                orderDTOs,
                "Order is fetched successfully",
                totalCount,
                request.PageNumber,
                request.PageSize
                
            
                
                );
        }
    }

}
