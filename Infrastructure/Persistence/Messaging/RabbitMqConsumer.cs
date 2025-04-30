/*using Application.Interfaces.Services.Messaging;
using Microsoft.EntityFrameworkCore.Metadata;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;


namespace Infrastructure.Persistence.Messaging
{
    public class RabbitMqConsumer : IRabbitMqConsumer
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMqConsumer(IConfiguration configuration)
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMq:HostName"] // Get hostname from appsettings.json
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public void StartConsuming(string queueName, Action<string> onMessageReceived)
        {
            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                // Pass the message to the provided callback
                onMessageReceived(message);
            };

            _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
        }
    }
}
*/