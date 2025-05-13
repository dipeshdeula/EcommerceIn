using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Features.CartItemFeat.Commands;
using Application.Features.CartItemFeat.Queries;
using Application.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;
using System.Threading.Channels;

public class RabbitMqConsumerService : BackgroundService
{
    private const int MAX_RETRIES = 5; // Maximum number of retries
    private const int INITIAL_RETRY_DELAY_MS = 1000; // Initial delay in milliseconds
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
                await ProcessMessageWithRetry(message, properties, correlationId, replyTo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message after retries. CorrelationId: {CorrelationId}", correlationId);
                await SendErrorResponse(replyTo, correlationId, "Failed to process message after retries.");
            }
        });

        return Task.CompletedTask;
    }

    private async Task ProcessMessageWithRetry(string message, IBasicProperties properties, string correlationId, string replyTo)
    {
        int retryCount = 0;
        int delay = INITIAL_RETRY_DELAY_MS;

        while (retryCount < MAX_RETRIES)
        {
            try
            {
                await ProcessMessage(message, properties, correlationId, replyTo);
                return; // Exit the loop if processing succeeds
            }
            catch (DbUpdateConcurrencyException ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "Retry {RetryCount}/{MaxRetries} for message. CorrelationId: {CorrelationId}", retryCount, MAX_RETRIES, correlationId);

                if (retryCount >= MAX_RETRIES)
                {
                    throw; // Rethrow the exception after max retries
                }

                await Task.Delay(delay); // Wait before retrying
                delay *= 2; // Exponential backoff
            }
        }
    }

    private async Task ProcessMessage(string message, IBasicProperties properties, string correlationId, string replyTo)
    {
        using var scope = _serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var request = JsonConvert.DeserializeObject<CreateCartItemCommand>(message);
        if (request == null)
        {
            await SendErrorResponse(replyTo, correlationId, "Invalid message format.");
            return;
        }

        var dbContext = services.GetRequiredService<MainDbContext>();
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                var productRepository = services.GetRequiredService<IProductRepository>();
                var cartItemRepository = services.GetRequiredService<ICartItemRepository>();

                // Get product
                var product = await productRepository.FindByIdAsync(request.ProductId);
                if (product == null)
                {
                    await SendErrorResponse(replyTo, correlationId, $"Product not found: {request.ProductId}");
                    return;
                }

                

                // Check stock
                if (product.AvailableStock < request.Quantity)
                {
                    await SendErrorResponse(replyTo, correlationId,
                        $"Insufficient stock for product: {product.Name}. Available: {product.AvailableStock}");
                    return;
                }

                // check time constraint
                

                // Reserve stock
                product.ReservedStock += request.Quantity;
                await productRepository.UpdateAsync(product);

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

                // Load navigation properties
                await cartItemRepository.LoadNavigationProperties(cartItem);

                // Send success response
                var responseData = cartItem.ToDTO();
                await SendSuccessResponse(replyTo, correlationId, responseData);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction failed for request {CorrelationId}", correlationId);
                await SendErrorResponse(replyTo, correlationId, "Failed to process cart item request.");
                throw; // Rethrow to trigger retry logic
            }
        });
    }

    private async Task SendSuccessResponse(string replyTo, string correlationId, CartItemDTO data)
    {
        var response = Result<CartItemDTO>.Success(data, "Cart item request has been processed successfully.");
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

