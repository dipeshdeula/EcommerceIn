/*using Application.Interfaces.Services.Messaging;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Persistence.Messaging
{
    public class RabbitMqPublisher : IRabbitMqPublisher
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _queueName;

        public RabbitMqPublisher(IConfiguration configuration)
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMq:HostName"] // Get hostname from appsettings.json
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _queueName = configuration["RabbitMq:QueueName"]; // Get queue name from appsettings.json
            _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        }

        public void Publish<T>(string queueName, T message)
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
        }
    }
}
*/