using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Messaging;

public class NotificationRabbitMqPublisher : INotificationRabbitMqPublisher,IDisposable
{
    private readonly string _hostname;
    private readonly string _queueName;
    private readonly string _exchangeName;
    private readonly IConnection _connection;

    public NotificationRabbitMqPublisher(string hostname, string queueName, string exchangeName = "")
    {
        _hostname = hostname;
        _queueName = queueName;
        _exchangeName = exchangeName;

        var factory = new ConnectionFactory { HostName = _hostname };
        _connection = factory.CreateConnection();
    }

    public void PublishMessage<T>(T message)
    {
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