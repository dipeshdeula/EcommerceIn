using Application.Dto;
using Application.Features.CartItemFeat.Commands;
using Application.Interfaces.Repositories;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Infrastructure.Persistence.Services
{
    public class RabbitMqConsumerService : BackgroundService
    {
        private readonly IRabbitMqConsumer _consumer;
        private readonly ICartItemRepository _cartItemRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUserRepository _userRepository;
        private readonly string _queueName;

        public RabbitMqConsumerService(
            IRabbitMqConsumer consumer,
            ICartItemRepository cartItemRepository,
            IProductRepository productRepository,
            IUserRepository userRepository,
            IConfiguration configuration)
        {
            _consumer = consumer;
            _cartItemRepository = cartItemRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
            _queueName = configuration["RabbitMQ:QueueName"] ?? "ReserveStockQueue";
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.StartConsuming(_queueName, async message =>
            {
                var request = JsonConvert.DeserializeObject<CreateCartItemCommand>(message);
                if (request != null)
                {

                    var user = await _userRepository.FindByIdAsync(request.UserId);
                    if (user == null) return;

                    var product = await _productRepository.FindByIdAsync(request.ProductId);
                    if (product == null) return;

                    if (product.StockQuantity - product.ReservedStock < request.Quantity) return;

                    product.ReservedStock += request.Quantity;
                    await _productRepository.UpdateAsync(product);

                    var cartItem = new CartItem
                    {
                        UserId = request.UserId,
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                        User = user,
                        Product = product
                    };


                    await _cartItemRepository.AddAsync(cartItem);
                }
            });

            return Task.CompletedTask;
        }
    }
}
