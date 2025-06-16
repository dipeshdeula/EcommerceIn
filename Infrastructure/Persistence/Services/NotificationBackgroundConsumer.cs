using Application.Features.OrderFeat.Events;
using Infrastructure.Persistence.Configurations;
using Infrastructure.Persistence.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Services;


public class NotificationBackgroundConsumer : BackgroundService
{
    private readonly OrderConfirmedConsumer _orderConfirmedConsumer;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseUrl;
    private readonly string _userNotificationApiUrl;
    private readonly string _broadcastApiUrl;
    private readonly string _adminNotificationApiUrl;
    private readonly ILogger<NotificationBackgroundConsumer> _logger;
    private readonly IServiceTokenService _serviceTokenProvider;
    public NotificationBackgroundConsumer(
        OrderConfirmedConsumer consumer,
        IHttpClientFactory httpClientFactory,
        IOptions<ApiConfig> apiConfig,
        ILogger<NotificationBackgroundConsumer> logger,
        IServiceTokenService serviceTokenProvider)
    {
        _orderConfirmedConsumer = consumer;
        _httpClientFactory = httpClientFactory;
        _baseUrl = apiConfig.Value.BaseUrl;
        _userNotificationApiUrl = $"{_baseUrl}send-to-user";
        _broadcastApiUrl = $"{_baseUrl}send-to-all";
        _adminNotificationApiUrl = $"{_baseUrl}send-to-admin";
        _logger = logger;
        _serviceTokenProvider = serviceTokenProvider;

        _baseUrl = apiConfig.Value.BaseUrl;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const string functionName = nameof(ExecuteAsync);

        _orderConfirmedConsumer.ConsumeMessages(async message =>
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("{Function}: Received empty message.", functionName);
                return;
            }
            var jsonMessage = JObject.Parse(message);
            var messageType = jsonMessage.GetValue("Type")?.ToString();
            try
            {
                _logger.LogInformation("{Function}: Message received: {Message}", functionName, message);

                //var jsonMessage = JObject.Parse(message);
                var token = _serviceTokenProvider.GetServiceToken();

                using var httpClient = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post,
                    jsonMessage.TryGetValue("Email", out _) ? _userNotificationApiUrl : _broadcastApiUrl)
                {
                    Content = new StringContent(message, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.SendAsync(request, stoppingToken);
                var destination = request.RequestUri.ToString();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("{Function}: Successfully forwarded notification to {Destination}.", functionName, destination);
                }
                else
                {
                    _logger.LogError("{Function}: Failed to forward notification. Destination: {Destination}, Status Code: {StatusCode}", functionName, destination, response.StatusCode);
                }

                try
                {
                    var userNotification = jsonMessage.ToObject<OrderConfirmedEvent>();
                    _logger.LogInformation("{Function}: Parsed notification object: {@UserNotification}", functionName, userNotification);
                }
                catch
                {
                    _logger.LogWarning("{Function}: Could not parse message into UserNotificationRequest.", functionName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Function}: Exception occurred while processing message: {Message}", functionName, message);
            }
        });

        return Task.CompletedTask;
    }


    public override void Dispose()
    {
        _orderConfirmedConsumer.Dispose();
        base.Dispose();
    }
}
