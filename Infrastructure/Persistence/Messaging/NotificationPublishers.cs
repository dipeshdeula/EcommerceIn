namespace Infrastructure.Persistence.Messaging;

public class OrderConfirmedPublisher : NotificationRabbitMqPublisher
{
<<<<<<< HEAD
    public OrderConfirmedPublisher(IConfiguration configuration)
=======
    public OrderConfirmedPublisher(IConfiguration configuration) 
>>>>>>> main
        : base(configuration, "OrderConfirmedQueue") { }
}

public class OrderPlacedPublisher : NotificationRabbitMqPublisher
{
<<<<<<< HEAD
    public OrderPlacedPublisher(IConfiguration configuration)
=======
    public OrderPlacedPublisher(IConfiguration configuration) 
>>>>>>> main
        : base(configuration, "OrderPlacedQueue") { }
}
