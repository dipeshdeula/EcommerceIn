using Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Messaging;

public class OrderConfirmedPublisher : NotificationRabbitMqPublisher
{
    public OrderConfirmedPublisher() : base("localhost", "OrderConfirmedQueue") { }
}
public class OrderPlacedPublisher : NotificationRabbitMqPublisher
{
    public OrderPlacedPublisher() : base("localhost", "OrderPlacedQueue") { }
}