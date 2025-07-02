using Application.Common;
using Application.Dto.OrderDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.OrderFeat.Queries
{
    public record GetOrderByIdQuery (int id) : IRequest<Result<IEnumerable<OrderDTO>>>;

    

    public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<IEnumerable<OrderDTO>>>
    {
        private readonly IOrderRepository _orderRepository;
        public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
            
        }
        public async Task<Result<IEnumerable<OrderDTO>>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var orders = await _orderRepository.GetQueryable().Where(
                    o => o.Id == request.id && !o.IsDeleted)
                     .Include(o => o.Items).ToListAsync(cancellationToken);

                if (!orders.Any())
                {
                    return Result<IEnumerable<OrderDTO>>.Failure("Order not found");
                }

                var orderDTOs = orders.Select(o => o.ToDTO()).ToList();
                return Result<IEnumerable<OrderDTO>>.Success(orderDTOs, "order fetched successfully");
            }
            catch (Exception ex)
            {
                throw new Exception("Product not found",ex);
            }
        }
    }

}
