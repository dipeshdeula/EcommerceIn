using Application.Common;
using Application.Dto.CartItemDTOs;
using Application.Dto.OrderDTOs;
using Application.Extension;
using Application.Features.CartItemFeat.Commands;
using Application.Features.CartItemFeat.Queries;
using Application.Interfaces.Repositories;
using Infrastructure.Hubs;
using Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using System.Net.Http.Headers;
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
    private readonly string _queueNameOrderPlace;
    private readonly IHubContext<AdminNotificationHub> _adminContext;
    private readonly IHubContext<UserNotificationHub> _userNotificationHub;
    private readonly IEmailService _emailService;
    private readonly ITokenService _serviceTokenProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseUrl;

    public RabbitMqConsumerService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<RabbitMqConsumerService> logger,
        IRabbitMqConsumer consumer,
        IHubContext<AdminNotificationHub> adminHubContext,
        IHubContext<UserNotificationHub> userHubContext,
        IEmailService emailService
,
        ITokenService serviceTokenProvider
,
        IHttpClientFactory httpClientFactory,
        IOptions<ApiConfig> apiConfig)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _consumer = consumer;
        _queueName = configuration["RabbitMQ:QueueName"] ?? "ReserveStockQueue";
        _adminContext = adminHubContext;
        _userNotificationHub = userHubContext;
        _queueNameOrderPlace = configuration["RabbitMQ:OrderPlacedQueue"] ?? "OrderPlacedQueue";
        _userNotificationHub = userHubContext;
        _emailService = emailService;
        _serviceTokenProvider = serviceTokenProvider;
        _httpClientFactory = httpClientFactory;

        _baseUrl = apiConfig.Value.BaseUrl;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMqConsumerService started.");
        const string functionName = nameof(ExecuteAsync);
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

          // Listen to OrderPlacedQueue for admin notifications
        _consumer.StartConsuming(_queueNameOrderPlace, async (message, properties) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogWarning("{Function}: Received empty message.", functionName);
                    return;
                }
                //var orderPlacedEvent = JsonConvert.DeserializeObject<OrderPlacedEventDTO>(message);

                var jsonMessage = JObject.Parse(message);

                var token = _serviceTokenProvider.GetServiceToken();
                using var httpClient = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post,
                    $"{_baseUrl}send-to-admin")
                {
                    Content = new StringContent(message, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.SendAsync(request, stoppingToken);

                // Notify all admins via SignalR
                
                var destination = request.RequestUri.ToString();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully forwarded notification to {Destination}.", destination);
                }
                else
                {
                    _logger.LogError("Failed to forward notification. Destination: {Destination}, Status Code: {StatusCode}", destination, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify admin for order placed event.");
            }
        });

        // Add in ExecuteAsync or similar
        _consumer.StartConsuming("OrderConfirmedQueue", async (message, properties) =>
        {
            try
            {
                var orderConfirmedEvent = JsonConvert.DeserializeObject<OrderConfirmedEventDTO>(message);

                // Real-time notification via SignalR
                await _userNotificationHub.Clients.User(orderConfirmedEvent.UserId.ToString())
                    .SendAsync("OrderConfirmed", new
                    {
                        OrderId = orderConfirmedEvent.OrderId,
                        Message = $"Your order #{orderConfirmedEvent.OrderId} has been confirmed and will be delivered in approximately {orderConfirmedEvent.EtaMinutes} minutes."
                    });

                // Email notification
                await _emailService.SendEmailAsync(
                    orderConfirmedEvent.UserEmail,
                    "Order Confirmed",
                    $"Hello {orderConfirmedEvent.UserName},<br>Your order #{orderConfirmedEvent.OrderId} has been confirmed and will be delivered in approximately {orderConfirmedEvent.EtaMinutes} minutes.<br>Thank you for shopping with us!"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify user for order confirmed event.");
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

        var stockService = services.GetRequiredService<IStockReservationService>();
        var request = JsonConvert.DeserializeObject<CreateCartItemCommand>(message);
        if (request == null)
        {
            await SendErrorResponse(replyTo, correlationId, "Invalid message format.");
            return;
        }

        var result = await stockService.ReserveStockAsync(request, correlationId, replyTo);

        if (result.Succeeded)
            await SendSuccessResponse(replyTo, correlationId, result.Data);
        else
            await SendErrorResponse(replyTo, correlationId, result.Message);
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

