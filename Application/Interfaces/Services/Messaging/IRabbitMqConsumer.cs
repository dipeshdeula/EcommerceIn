using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services.Messaging
{
    public interface IRabbitMqConsumer
    {
        void StartConsuming(string queueName, Action<string> onMessageReceived);
    }
}
