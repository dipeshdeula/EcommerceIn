using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Messaging;

public class NotificationRabbitMqConsumer : IDisposable
{
    private readonly string _hostname;
    private readonly string _queueName;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public NotificationRabbitMqConsumer(string hostname, string queueName)
    {
        _hostname = hostname;
        _queueName = queueName;

        var factory = new ConnectionFactory { HostName = _hostname };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
    }

    public void ConsumeMessages(Action<string> onMessageReceived)
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            try
            {
                onMessageReceived(message);

                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
                //  reject the message (requeue or discard)
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
