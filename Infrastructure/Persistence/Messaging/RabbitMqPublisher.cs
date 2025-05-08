/*using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Messaging
{
    public class RabbitMQPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _responseTasks;

        public RabbitMQPublisher(IConfiguration configuration)
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:HostName"] ?? "localhost"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _responseTasks = new ConcurrentDictionary<string, TaskCompletionSource<object>>();
        }

        public void Publish<T>(string queueName, T message, string correlationId, string replyQueueName)
        {
            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var messageBody = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(messageBody);

            var properties = _channel.CreateBasicProperties();
            properties.CorrelationId = correlationId;
            properties.ReplyTo = replyQueueName;

            _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: properties, body: body);
        }

        public void Publish(string queueName, byte[] message, string correlationId, string replyQueueName)
        {
            var properties = _channel.CreateBasicProperties();
            properties.CorrelationId = correlationId;
            properties.ReplyTo = replyQueueName;

            _channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: properties,
                body: message
            );
        }

        public Task<object> WaitForResponseAsync(string replyQueueName, string correlationId, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<object>();
            _responseTasks[correlationId] = tcs;

            _channel.QueueDeclare(queue: replyQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    var response = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var result = JsonConvert.DeserializeObject<object>(response);

                    if (_responseTasks.TryRemove(correlationId, out var taskSource))
                    {
                        taskSource.SetResult(result);
                    }
                }
            };

            _channel.BasicConsume(queue: replyQueueName, autoAck: true, consumer: consumer);

            cancellationToken.Register(() =>
            {
                if (_responseTasks.TryRemove(correlationId, out var taskSource))
                {
                    taskSource.SetCanceled();
                }
            });

            return tcs.Task;
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
*/

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Messaging
{
    public class RabbitMQPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMQPublisher> _logger;
        private IConnection _connection;
        private IModel _channel;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _responseTasks = new();
        private bool _initialized = false;

        public RabbitMQPublisher(IConfiguration configuration, ILogger<RabbitMQPublisher> logger)
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

        public void Publish<T>(string queueName, T message, string correlationId, string replyQueueName)
        {
            if (!EnsureConnected())
            {
                _logger.LogWarning("Cannot publish message to {QueueName}: not connected to RabbitMQ", queueName);
                return;
            }

            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var messageBody = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(messageBody);

            var properties = _channel.CreateBasicProperties();
            properties.CorrelationId = correlationId;
            properties.ReplyTo = replyQueueName;

            _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: properties, body: body);
        }

        public void Publish(string queueName, byte[] message, string correlationId, string replyQueueName)
        {
            if (!EnsureConnected())
            {
                _logger.LogWarning("Cannot publish byte message to {QueueName}: not connected to RabbitMQ", queueName);
                return;
            }

            var properties = _channel.CreateBasicProperties();
            properties.CorrelationId = correlationId;
            properties.ReplyTo = replyQueueName;

            _channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: properties,
                body: message
            );
        }

        public Task<object> WaitForResponseAsync(string replyQueueName, string correlationId, CancellationToken cancellationToken)
        {
            if (!EnsureConnected())
            {
                _logger.LogWarning("Cannot wait for response: not connected to RabbitMQ");
                return Task.FromException<object>(new InvalidOperationException("Not connected to RabbitMQ"));
            }

            var tcs = new TaskCompletionSource<object>();
            _responseTasks[correlationId] = tcs;

            _channel.QueueDeclare(queue: replyQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    var response = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var result = JsonConvert.DeserializeObject<object>(response);

                    if (_responseTasks.TryRemove(correlationId, out var taskSource))
                    {
                        taskSource.SetResult(result);
                    }
                }
            };

            _channel.BasicConsume(queue: replyQueueName, autoAck: true, consumer: consumer);

            cancellationToken.Register(() =>
            {
                if (_responseTasks.TryRemove(correlationId, out var taskSource))
                {
                    taskSource.SetCanceled();
                }
            });

            return tcs.Task;
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}