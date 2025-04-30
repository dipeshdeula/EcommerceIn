using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services.Messaging
{
    public interface IRabbitMqPublisher
    {
        void Publish<T>(string queueName, T message);
    }
}
