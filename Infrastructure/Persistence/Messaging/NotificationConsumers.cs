using Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Messaging;

public class OrderConfirmedConsumer : NotificationRabbitMqConsumer
{
    public OrderConfirmedConsumer() : base("localhost", "OrderConfirmedQueue") { }
}
public class OrderPlacedConsumer : NotificationRabbitMqConsumer
{
    public OrderPlacedConsumer() : base("localhost", "OrderPlacedQueue") { }
}