using Application.Common;
using Application.Dto.CartItemDTOs;
using Application.Extension;
using Application.Features.CartItemFeat.Commands;
using Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services;

public class StockReservationService : IStockReservationService
{
    private readonly IProductRepository _productRepository;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly MainDbContext _dbContext;
    private readonly ILogger<StockReservationService> _logger;

    public StockReservationService(
        IProductRepository productRepository,
        ICartItemRepository cartItemRepository,
        MainDbContext dbContext,
        ILogger<StockReservationService> logger)
    {
        _productRepository = productRepository;
        _cartItemRepository = cartItemRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<CartItemDTO>> ReserveStockAsync(CreateCartItemCommand request, string correlationId, string replyTo)
    {
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var product = await _productRepository.FindByIdAsync(request.ProductId);
                if (product == null)
                    return Result<CartItemDTO>.Failure($"Product not found: {request.ProductId}");

                if (product.AvailableStock < request.Quantity)
                    return Result<CartItemDTO>.Failure($"Insufficient stock for product: {product.Name}");

                product.ReservedStock += request.Quantity;
                await _productRepository.UpdateAsync(product);

                var cartItem = new CartItem
                {
                    UserId = request.UserId,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity
                };

                await _cartItemRepository.AddAsync(cartItem);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                await _cartItemRepository.LoadNavigationProperties(cartItem);
                return Result<CartItemDTO>.Success(cartItem.ToDTO());
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error reserving stock.");
                return Result<CartItemDTO>.Failure("Failed to reserve stock.");
            }
        });
    }
}

