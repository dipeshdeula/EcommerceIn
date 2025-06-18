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
    private readonly IConfiguration _configuration;
    private readonly string _queueName;
    private readonly IConnection _connection;
    private readonly IModel _channel;


    public NotificationRabbitMqConsumer(IConfiguration configuration, string queueName)
    {
        _configuration = configuration;
        _queueName = queueName;
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
                    Console.WriteLine("Connecting to RabbitMQ using URI");
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

                Console.WriteLine("RabbitMQ HostName: " + _configuration["RabbitMQ:HostName"]);
                Console.WriteLine("RabbitMQ Uri: " + _configuration["RabbitMQ:Uri"]);

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                Console.WriteLine("Successfully connected to RabbitMQ");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect to RabbitMQ");
                throw new InvalidOperationException("Could not connect to RabbitMQ",ex);
            }
    }

    public void ConsumeMessages(Action<string> onMessageReceived)
    {
        if(_channel == null)
        {
            throw new InvalidOperationException("RabbitMQ channel is not initialized. Ensure that the connection is established successfully.");
        }
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
