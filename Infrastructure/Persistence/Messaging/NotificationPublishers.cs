namespace Infrastructure.Persistence.Messaging;

public class OrderConfirmedPublisher : NotificationRabbitMqPublisher
{
    public OrderConfirmedPublisher(IConfiguration configuration)
        : base(configuration, "OrderConfirmedQueue") { }
}

public class OrderPlacedPublisher : NotificationRabbitMqPublisher
{
    public OrderPlacedPublisher(IConfiguration configuration)
        : base(configuration, "OrderPlacedQueue") { }
}
