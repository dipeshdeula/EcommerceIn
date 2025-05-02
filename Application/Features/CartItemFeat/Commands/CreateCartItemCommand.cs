using Application.Common;
using Application.Dto;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Application.Features.CartItemFeat.Commands
{
    public record CreateCartItemCommand(
        int UserId,
        int ProductId,
        int Quantity
    ) : IRequest<Result<CartItemDTO>>;

    public class CreateCartItemCommandHandler : IRequestHandler<CreateCartItemCommand, Result<CartItemDTO>>
    {
        private readonly IRabbitMqPublisher _rabbitMqPublisher;
        private readonly string _queueName;

        public CreateCartItemCommandHandler(IRabbitMqPublisher rabbitMqPublisher, IConfiguration configuration)
        {
            _rabbitMqPublisher = rabbitMqPublisher;
            _queueName = configuration["RabbitMQ:QueueName"] ?? "ReserveStockQueue";
        }

        public Task<Result<CartItemDTO>> Handle(CreateCartItemCommand request, CancellationToken cancellationToken)
        {
            // Publish the request to RabbitMQ
            _rabbitMqPublisher.Publish(_queueName, request);

            // Simulate waiting for the consumer to process the message
            // In a real-world scenario, you might use a callback mechanism or a database update to track the result
            var cartItemDto = new CartItemDTO
            {
                UserId = request.UserId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                IsDeleted = false
            };

            return Task.FromResult(Result<CartItemDTO>.Success(cartItemDto, "Cart item request has been processed successfully."));
        }
    }
}
