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
    public record GetOrderByIdQuery (int id) : IRequest<Result<OrderDTO>>;

    public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDTO>>
    {
        private readonly IOrderRepository _orderRepository;
        public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
            
        }
        public async Task<Result<OrderDTO>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _orderRepository.GetQueryable().Where(
                    o => o.Id == request.id && !o.IsDeleted)
                     .Include(o => o.Items).FirstOrDefaultAsync();

                if (order is null)
                {
                    return Result<OrderDTO>.Failure("Order not found");
                }
                return Result<OrderDTO>.Success(order.ToDTO(), "order fetched successfully");
            }
            catch (Exception ex)
            {
                throw new Exception("Product not found",ex);
            }
        }
    }

}
