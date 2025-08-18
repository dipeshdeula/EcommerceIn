using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure.Persistence.Messaging;

public class NotificationRabbitMqConsumer : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly string _queueName;
    private  IConnection _connection;
    private  IModel _channel;
    private bool _isConnected = false;


    public NotificationRabbitMqConsumer(IConfiguration configuration, string queueName)
    {
        _configuration = configuration;
        _queueName = queueName;
        // try
        // {
        //     ConnectionFactory factory;

        //     // Check if we have a full AMQP URI
        //     var amqpUri = _configuration["RabbitMQ:Uri"];
        //     if (!string.IsNullOrEmpty(amqpUri))
        //     {
        //         // Use the URI directly
        //         factory = new ConnectionFactory
        //         {
        //             Uri = new Uri(amqpUri)
        //         };
        //         Console.WriteLine("Connecting to RabbitMQ using URI");
        //     }
        //     else
        //     {
        //         // Use individual component settings
        //         factory = new ConnectionFactory
        //         {
        //             HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
        //             UserName = _configuration["RabbitMQ:Username"] ?? "guest",
        //             Password = _configuration["RabbitMQ:Password"] ?? "guest",
        //             VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/"
        //         };

        //         // Enable SSL if configured
        //         if (bool.TryParse(_configuration["RabbitMQ:Ssl"], out bool useSsl) && useSsl)
        //         {
        //             factory.Ssl = new SslOption
        //             {
        //                 Enabled = true,
        //                 ServerName = factory.HostName
        //             };
        //         }
        //     }

        //     Console.WriteLine("RabbitMQ HostName: " + _configuration["RabbitMQ:HostName"]);
        //     Console.WriteLine("RabbitMQ Uri: " + _configuration["RabbitMQ:Uri"]);

        //     _connection = factory.CreateConnection();
        //     _channel = _connection.CreateModel();
        //     _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        //     Console.WriteLine("Successfully connected to RabbitMQ");
        // }
        // catch (Exception ex)
        // {
        //     Console.WriteLine("Failed to connect to RabbitMQ");
        //     throw new InvalidOperationException("Could not connect to RabbitMQ", ex);
        // }
    }

    private void EnsureConnected()
    {
        try
        {
            if (_isConnected) return;

            ConnectionFactory factory;
            var amqpUri = _configuration["RabbitMQ:Uri"];
            if (!string.IsNullOrEmpty(amqpUri))
            {
                factory = new ConnectionFactory
                {
                    Uri = new Uri(amqpUri)
                };
            }
            else
            {
                factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                    UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                    Password = _configuration["RabbitMQ:Password"] ?? "guest",
                    VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/"
                };

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
                Console.WriteLine("RabbitMQ UserName:" + _configuration["RabbitMQ:Username"]);
                Console.WriteLine("RabbitMQ Password:" + _configuration["RabbitMQ:Password"]);


            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _isConnected = true;
            Console.WriteLine("Successfully connected to RabbitMQ");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to connect to RabbitMQ");
            throw new InvalidOperationException("Could not connect to RabbitMQ", ex);
        }


    }

    public void ConsumeMessages(Action<string> onMessageReceived)
    {
        EnsureConnected();
        if (_channel == null)
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
