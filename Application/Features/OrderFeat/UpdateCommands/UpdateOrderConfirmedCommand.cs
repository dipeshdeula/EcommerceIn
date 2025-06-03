using Application.Common;
using Application.Dto;
using Application.Dto.OrderDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.OrderFeat.UpdateCommands
{
    public record UpdateOrderConfirmedCommand(
        int OrderId,        
        bool IsConfirmed
        ) : IRequest<Result<bool>>;

    public class UpdateOrderConfirmedCommandHandler : IRequestHandler<UpdateOrderConfirmedCommand, Result<bool>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly IRabbitMqPublisher _rabbitMqPublisher;
       public UpdateOrderConfirmedCommandHandler(
            IOrderRepository orderRepository,
            IUserRepository userRepository,
            IHttpContextAccessor httpContextAccessor,
            IEmailService emailService,
            IRabbitMqPublisher rabbitMqPublisher
        )
        {
            _orderRepository = orderRepository;
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
            _rabbitMqPublisher = rabbitMqPublisher;
        }
        public async Task<Result<bool>> Handle(UpdateOrderConfirmedCommand request, CancellationToken cancellationToken)
        {
            var order = await _orderRepository.FindByIdAsync( request.OrderId );
            if (order == null)
            {
                return Result<bool>.Failure("Order Id not found");
            }

            var userClaims = _httpContextAccessor.HttpContext?.User;
            if (userClaims == null)
            {
                return Result<bool>.Failure("Unauthorized");
            }

            var role = userClaims.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (role != UserRoles.Admin.ToString() && role != UserRoles.SuperAdmin.ToString())
                return Result<bool>.Failure("Only Admin or SuperAdmin can confirm orders.");

            order.Status = request.IsConfirmed ? "Confirmed" : "Pending";
            order.IsConfirmed = request.IsConfirmed;
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepository.UpdateAsync(order, cancellationToken);

            var user = await _userRepository.FindByIdAsync(order.UserId);
            if (user == null)
            {
                return Result<bool>.Failure("User not found for the order.");
            }

            int etaMinutes = 25; // hard cocded for now

            var orderConfirmedEvent = new OrderConfirmedEventDTO
            {
                OrderId = order.Id,
                UserId = user.Id,
                UserEmail = user.Email,
                UserName = user.Name,
                EtaMinutes = etaMinutes
            };

            _rabbitMqPublisher.Publish("OrderConfirmedQueue", orderConfirmedEvent, Guid.NewGuid().ToString(), null);


            return Result<bool>.Success(true, "Order status updated.");

            
            

           
        }
    }

}
