using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Messaging;

public class NotificationRabbitMqPublisher : INotificationRabbitMqPublisher, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly string _hostname;
    private readonly string _queueName;
    private readonly string _exchangeName;
    private readonly IConnection _connection;

    public NotificationRabbitMqPublisher(IConfiguration configuration, string queueName, string exchangeName = "")
    {
        _configuration = configuration;
        _queueName = queueName;
        _exchangeName = exchangeName;

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

            _connection = factory.CreateConnection();
            Console.WriteLine("Successfully connected to RabbitMQ");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to connect to RabbitMQ: " + ex.Message);
            throw new InvalidOperationException("Could not connect to RabbitMQ", ex);

        }
    }

    public void PublishMessage<T>(T message)
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("RabbitMQ connection is not established.");
        }
        const string functionName = nameof(PublishMessage);

        using var channel = _connection.CreateModel();
        channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        var messageBody = JsonConvert.SerializeObject(message);
        var body = Encoding.UTF8.GetBytes(messageBody);

        Console.WriteLine($"{functionName}: Publishing message to RabbitMQ");
        Console.WriteLine($"{functionName}: Object: {JsonConvert.SerializeObject(message, Formatting.Indented)}");
        Console.WriteLine($"{functionName}: Serialized: {messageBody}");

        channel.BasicPublish(exchange: _exchangeName, routingKey: _queueName, basicProperties: null, body: body);
    }


    public void Dispose()
    {
        _connection?.Dispose();

    }
}