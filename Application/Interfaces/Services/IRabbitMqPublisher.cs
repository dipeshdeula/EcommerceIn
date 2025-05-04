using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IRabbitMqPublisher
    {
        void Publish<T>(string queueName, T message, string correlationId, string replyQueueName);
        void Publish(string queueName, byte[] message, string correlationId, string replyQueueName);
        Task<object> WaitForResponseAsync(string replyQueueName, string correlationId, CancellationToken cancellationToken);
    }
}
