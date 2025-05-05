using Application.Common;
using Application.Dto;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

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
        private readonly string _replyQueueName;

        public CreateCartItemCommandHandler(IRabbitMqPublisher rabbitMqPublisher, IConfiguration configuration)
        {
            _rabbitMqPublisher = rabbitMqPublisher;
            _queueName = configuration["RabbitMQ:QueueName"] ?? "ReserveStockQueue";
            _replyQueueName = configuration["RabbitMQ:ReplyQueueName"] ?? "ReplyQueue";
        }

        public async Task<Result<CartItemDTO>> Handle(CreateCartItemCommand request, CancellationToken cancellationToken)
        {
            var correlationId = Guid.NewGuid().ToString();
            var replyQueueName = "ReplyQueue";

            // Publish the request to RabbitMQ
            _rabbitMqPublisher.Publish(_queueName, request, correlationId, _replyQueueName);

            // Wait for the response from the reply-to queue
            try
            {
                var response = await _rabbitMqPublisher.WaitForResponseAsync(_replyQueueName, correlationId, cancellationToken);
                return JsonConvert.DeserializeObject<Result<CartItemDTO>>(response.ToString());
            }
            catch (TaskCanceledException)
            {
                return Result<CartItemDTO>.Failure("Timeout waiting for the consumer to process the request.");
            }
        }
    }
}
