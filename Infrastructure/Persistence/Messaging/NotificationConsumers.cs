<<<<<<< HEAD
﻿namespace Infrastructure.Persistence.Messaging;

public class OrderConfirmedConsumer : NotificationRabbitMqConsumer
{
    public OrderConfirmedConsumer(IConfiguration configuration)
=======
﻿using Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Messaging;
public class OrderConfirmedConsumer : NotificationRabbitMqConsumer
{
    public OrderConfirmedConsumer(IConfiguration configuration) 
>>>>>>> main
        : base(configuration, "OrderConfirmedQueue") { }
}

public class OrderPlacedConsumer : NotificationRabbitMqConsumer
{
<<<<<<< HEAD
    public OrderPlacedConsumer(IConfiguration configuration)
=======
    public OrderPlacedConsumer(IConfiguration configuration) 
>>>>>>> main
        : base(configuration, "OrderPlacedQueue") { }
}
