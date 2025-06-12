using Application.Interfaces.Services;
using Application.Extension;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services
{
    public class CartStockService : ICartStockService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CartStockService> _logger;

        public CartStockService(IUnitOfWork unitOfWork, ILogger<CartStockService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<StockReservationResult> TryReserveStockAsync(int productId, int quantity, int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    return StockReservationResult.Failed("Product not found");
                }

                // Check available stock
                var availableStock = product.StockQuantity - product.ReservedStock;
                if (availableStock < quantity)
                {
                    return StockReservationResult.Failed(
                        $"Insufficient stock. Available: {availableStock}, Requested: {quantity}",
                        availableStock);
                }

                // Reserve stock
                product.ReservedStock += quantity;

                // Generate reservation token
                var reservationToken = $"RSV_{userId}_{productId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";

                await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Reserved {Quantity} units for product {ProductId}, user {UserId}. Token: {Token}",
                    quantity, productId, userId, reservationToken);

                return StockReservationResult.Succeeded(reservationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reserving stock for product {ProductId}", productId);
                return StockReservationResult.Failed("Error reserving stock");
            }
        }

        public async Task<bool> ReleaseStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found for stock release", productId);
                    return false;
                }

                if (product.ReservedStock < quantity)
                {
                    _logger.LogWarning("Cannot release {Quantity} units for product {ProductId}. Reserved: {Reserved}",
                        quantity, productId, product.ReservedStock);
                    return false;
                }

                // Release reserved stock
                product.ReservedStock -= quantity;

                await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Released {Quantity} reserved units for product {ProductId}. Reserved now: {Reserved}",
                    quantity, productId, product.ReservedStock);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing stock for product {ProductId}", productId);
                return false;
            }
        }

        public async Task<bool> ConfirmStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found for stock confirmation", productId);
                    return false;
                }

                if (product.ReservedStock < quantity || product.StockQuantity < quantity)
                {
                    _logger.LogWarning("Cannot confirm {Quantity} units for product {ProductId}. Reserved: {Reserved}, Stock: {Stock}",
                        quantity, productId, product.ReservedStock, product.StockQuantity);
                    return false;
                }

                // Confirm stock (reduce both reserved and actual stock)
                product.ReservedStock -= quantity;
                product.StockQuantity -= quantity;

                await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Confirmed {Quantity} units for product {ProductId}. Stock: {Stock}, Reserved: {Reserved}",
                    quantity, productId, product.StockQuantity, product.ReservedStock);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming stock for product {ProductId}", productId);
                return false;
            }
        }

        public async Task<bool> UpdateReservationAsync(int productId, int oldQuantity, int newQuantity, CancellationToken cancellationToken = default)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found for reservation update", productId);
                    return false;
                }

                var quantityDiff = newQuantity - oldQuantity;

                if (quantityDiff > 0)
                {
                    // Need to reserve more stock
                    var availableStock = product.StockQuantity - product.ReservedStock;
                    if (availableStock < quantityDiff)
                    {
                        _logger.LogWarning("Cannot reserve additional {Quantity} units for product {ProductId}. Available: {Available}",
                            quantityDiff, productId, availableStock);
                        return false;
                    }
                    product.ReservedStock += quantityDiff;
                }
                else if (quantityDiff < 0)
                {
                    // Release some reserved stock
                    var releaseQuantity = Math.Abs(quantityDiff);
                    if (product.ReservedStock >= releaseQuantity)
                    {
                        product.ReservedStock -= releaseQuantity;
                    }
                }

                await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated reservation for product {ProductId}: {OldQty} → {NewQty}. Reserved now: {Reserved}",
                    productId, oldQuantity, newQuantity, product.ReservedStock);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reservation for product {ProductId}", productId);
                return false;
            }
        }
    }
}