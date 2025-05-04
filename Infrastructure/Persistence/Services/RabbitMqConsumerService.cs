/*using Application.Common;
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
*/

using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Features.CartItemFeat.Commands;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;
using System.Transactions;

public class RabbitMqConsumerService : BackgroundService
{
    private const int MAX_RETRIES = 3;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly IRabbitMqConsumer _consumer;
    private readonly string _queueName;

    public RabbitMqConsumerService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<RabbitMqConsumerService> logger,
        IRabbitMqConsumer consumer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _consumer = consumer;
        _queueName = configuration["RabbitMQ:QueueName"] ?? "ReserveStockQueue";
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMqConsumerService started.");

        _consumer.StartConsuming(_queueName, async (message, properties) =>
        {
            var correlationId = properties.CorrelationId;
            var replyTo = properties.ReplyTo;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var services = scope.ServiceProvider;

                var request = JsonConvert.DeserializeObject<CreateCartItemCommand>(message);
                if (request == null)
                {
                    await SendErrorResponse(replyTo, correlationId, "Invalid message format");
                    return;
                }

                var dbContext = services.GetRequiredService<MainDbContext>();
                var productRepository = services.GetRequiredService<IProductRepository>();
                var cartItemRepository = services.GetRequiredService<ICartItemRepository>();

                await using var transaction = await dbContext.Database.BeginTransactionAsync();

                try
                {
                    // Get fresh product instance with tracking
                    var product = await productRepository.FindByIdAsync(request.ProductId);
                    if (product == null)
                    {
                        await SendErrorResponse(replyTo, correlationId, $"Product not found: {request.ProductId}");
                        return;
                    }

                    // Check available stock with retry logic
                    var retryCount = 0;
                    while (retryCount < MAX_RETRIES)
                    {
                        try
                        {
                            if (product.AvailableStock < request.Quantity)
                            {
                                await SendErrorResponse(replyTo, correlationId,
                                    $"Insufficient stock for product: {product.Name}. Available: {product.AvailableStock}");
                                return;
                            }

                            product.ReservedStock += request.Quantity;
                            await productRepository.UpdateAsync(product);
                            break;
                        }
                        catch (DbUpdateConcurrencyException ex)
                        {
                            _logger.LogWarning("Concurrency conflict detected. Retry attempt {RetryCount}", retryCount);
                            await productRepository.ReloadAsync(product);
                            retryCount++;

                            if (retryCount >= MAX_RETRIES)
                            {
                                _logger.LogError(ex, "Max retries reached for product {ProductId}", request.ProductId);
                                await SendErrorResponse(replyTo, correlationId,
                                    "Could not reserve stock due to high demand. Please try again.");
                                return;
                            }
                        }
                    }

                    // Create cart item
                    var cartItem = new CartItem
                    {
                        UserId = request.UserId,
                        ProductId = request.ProductId,
                        Quantity = request.Quantity
                    };

                    await cartItemRepository.AddAsync(cartItem);
                    await dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Load navigation properties explicitly
                    await cartItemRepository.LoadNavigationProperties(cartItem);

                    // Send success response with proper DTO
                    var responseData = cartItem.ToDTO();
                    await SendSuccessResponse(replyTo, correlationId, responseData);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Transaction failed for request {CorrelationId}", correlationId);
                    await SendErrorResponse(replyTo, correlationId, "Failed to process cart item request");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {CorrelationId}", correlationId);
                await SendErrorResponse(replyTo, correlationId, "Internal server error");
            }
        });

        return Task.CompletedTask;
    }
    private async Task SendSuccessResponse(string replyTo, string correlationId, CartItemDTO data)
    {
        var response = Result<CartItemDTO>.Success(data);
        await SendResponse(replyTo, correlationId, response);
    }

    private async Task SendErrorResponse(string replyTo, string correlationId, string error)
    {
        var response = Result<CartItemDTO>.Failure(error);
        await SendResponse(replyTo, correlationId, response);
    }

    private async Task SendResponse<T>(string replyTo, string correlationId, Result<T> response)
    {
        if (string.IsNullOrEmpty(replyTo)) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

            var json = JsonConvert.SerializeObject(response);
            var body = Encoding.UTF8.GetBytes(json);

            publisher.Publish(
                queueName: replyTo,
                message: body,
                correlationId: correlationId,
                replyQueueName: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send response for {CorrelationId}", correlationId);
        }
        await Task.CompletedTask;
    }

}