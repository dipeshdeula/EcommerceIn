using Newtonsoft.Json;
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
