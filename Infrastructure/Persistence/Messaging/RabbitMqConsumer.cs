using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure.Persistence.Messaging
{
    public class RabbitMQConsumer : IRabbitMqConsumer, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMQConsumer(IConfiguration configuration)
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:HostName"] ?? "localhost"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public void StartConsuming(string queueName, Func<string, Task> onMessageReceived)
        {
            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                try
                {
                    await onMessageReceived(message);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch
                {
                    // Reject the message and requeue it
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
