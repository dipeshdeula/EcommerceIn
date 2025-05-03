using RabbitMQ.Client;
using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IRabbitMqConsumer
    {
        void StartConsuming(string queueName, Func<string, IBasicProperties, Task> onMessageReceived);
    }
}

