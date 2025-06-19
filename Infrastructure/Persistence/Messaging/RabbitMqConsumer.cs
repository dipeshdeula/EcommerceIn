using Application.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Messaging
{
     public class RabbitMQConsumer : IRabbitMqConsumer, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMQConsumer> _logger;
        private IConnection _connection;
        private IModel _channel;
        private bool _initialized = false;

        public RabbitMQConsumer(IConfiguration configuration, ILogger<RabbitMQConsumer> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private bool EnsureConnected()
        {
            if (_initialized && _connection?.IsOpen == true)
                return true;

            try
            {
                ConnectionFactory factory;

                // Check if we have a full AMQP URI
                var amqpUri = _configuration["RabbitMQ:Uri"];
                if (!string.IsNullOrEmpty(amqpUri))
                {
                    // Use the URI directly
                    factory = new ConnectionFactory
                    {
                        Uri = new Uri(amqpUri)
                    };
                    _logger.LogInformation("Connecting to RabbitMQ using URI");
                }
                else
                {
                    // Use individual component settings
                    factory = new ConnectionFactory
                    {
                        HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                        UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                        Password = _configuration["RabbitMQ:Password"] ?? "guest",
                        VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/"
                    };

                    // Enable SSL if configured
                    if (bool.TryParse(_configuration["RabbitMQ:Ssl"], out bool useSsl) && useSsl)
                    {
                        factory.Ssl = new SslOption
                        {
                            Enabled = true,
                            ServerName = factory.HostName
                        };
                    }
                }

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _initialized = true;
                _logger.LogInformation("Successfully connected to RabbitMQ");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to RabbitMQ");
                return false;
            }
        }

        public void StartConsuming(string queueName, Func<string, IBasicProperties, Task> onMessageReceived)
        {
            if (!EnsureConnected())
            {
                _logger.LogWarning("Cannot start consuming from queue {QueueName}: not connected to RabbitMQ", queueName);
                return;
            }

            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                try
                {
                    await onMessageReceived(message, ea.BasicProperties);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing RabbitMQ message");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}