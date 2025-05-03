using Application.Common;
using Application.Features.CartItemFeat.Commands;
using Application.Interfaces.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

public class RabbitMqConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly IModel _channel; // Add the RabbitMQ channel
    private readonly string _queueName;

    public RabbitMqConsumerService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<RabbitMqConsumerService> logger,
        IModel channel) // Inject the RabbitMQ channel
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _channel = channel; // Initialize the channel
        _queueName = configuration["RabbitMQ:QueueName"] ?? "ReserveStockQueue";
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMqConsumerService started.");

        using (var scope = _serviceProvider.CreateScope())
        {
            var consumer = scope.ServiceProvider.GetRequiredService<IRabbitMqConsumer>();

            consumer.StartConsuming(_queueName, async (message, properties) =>
            {
                var correlationId = properties.CorrelationId;
                var replyTo = properties.ReplyTo;

                try
                {
                    using (var innerScope = _serviceProvider.CreateScope())
                    {
                        var cartItemRepository = innerScope.ServiceProvider.GetRequiredService<ICartItemRepository>();
                        var productRepository = innerScope.ServiceProvider.GetRequiredService<IProductRepository>();
                        var userRepository = innerScope.ServiceProvider.GetRequiredService<IUserRepository>();

                        var request = JsonConvert.DeserializeObject<CreateCartItemCommand>(message);
                        if (request == null)
                        {
                            await SendErrorResponse(replyTo, correlationId, "Invalid message format.");
                            return;
                        }

                        var user = await userRepository.FindByIdAsync(request.UserId);
                        if (user == null)
                        {
                            await SendErrorResponse(replyTo, correlationId, $"User not found: {request.UserId}");
                            return;
                        }

                        var product = await productRepository.FindByIdAsync(request.ProductId);
                        if (product == null)
                        {
                            await SendErrorResponse(replyTo, correlationId, $"Product not found: {request.ProductId}");
                            return;
                        }

                        if (product.StockQuantity - product.ReservedStock < request.Quantity)
                        {
                            await SendErrorResponse(replyTo, correlationId, $"Insufficient stock for product: {request.ProductId}");
                            return;
                        }

                        product.ReservedStock += request.Quantity;
                        await productRepository.UpdateAsync(product);

                        var cartItem = new CartItem
                        {
                            UserId = request.UserId,
                            ProductId = request.ProductId,
                            Quantity = request.Quantity,
                            User = user,
                            Product = product
                        };

                        await cartItemRepository.AddAsync(cartItem);

                        await SendSuccessResponse(replyTo, correlationId, new
                        {
                            id = cartItem.Id,
                            userId = cartItem.UserId,
                            productId = cartItem.ProductId,
                            quantity = cartItem.Quantity,
                            isDeleted = cartItem.IsDeleted,
                            user = new
                            {
                                id = cartItem.User.Id,
                                name = cartItem.User.Name,
                                email = cartItem.User.Email
                            },
                            product = new
                            {
                                id = cartItem.Product.Id,
                                name = cartItem.Product.Name,
                                stockQuantity = cartItem.Product.StockQuantity,
                                reservedStock = cartItem.Product.ReservedStock
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message.");
                    await SendErrorResponse(replyTo, correlationId, "An unexpected error occurred while processing the request.");
                }
            });
        }

        return Task.CompletedTask;
    }

    private async Task SendErrorResponse(string replyTo, string correlationId, string errorMessage)
    {
        var response = Result<object>.Failure(errorMessage);
        await SendResponse(replyTo, correlationId, response);
    }

    private async Task SendSuccessResponse(string replyTo, string correlationId, object data)
    {
        var response = Result<object>.Success(data, "Cart item request has been processed successfully.");
        await SendResponse(replyTo, correlationId, response);
    }

    private async Task SendResponse(string replyTo, string correlationId, Result<object> response)
    {
        if (string.IsNullOrEmpty(replyTo))
        {
            _logger.LogWarning("Reply-to queue is not specified. Response will not be sent.");
            return;
        }

        var responseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
        var responseProps = _channel.CreateBasicProperties();
        responseProps.CorrelationId = correlationId;

        _channel.BasicPublish(exchange: "", routingKey: replyTo, basicProperties: responseProps, body: responseBytes);
        await Task.CompletedTask;
    }
}
