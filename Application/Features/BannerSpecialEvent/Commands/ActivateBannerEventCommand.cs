////filepath: e:\EcomerceDeployPostgres\EcommerceBackendAPI\Application\Features\BannerSpecialEvent\Commands\ActivateBannerEventCommand.cs
using Application.Common;
using Application.Dto.BannerEventSpecialDTOs;
using Application.Extension;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums.BannerEventSpecial;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.BannerSpecialEvent.Commands
{
    public record ActivateBannerEventCommand(
        int BannerEventId,
        bool IsActive
    ) : IRequest<Result<BannerEventSpecialDTO>>;

    public class ActivateBannerEventCommandHandler : IRequestHandler<ActivateBannerEventCommand, Result<BannerEventSpecialDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductPricingService _pricingService;
        private readonly ILogger<ActivateBannerEventCommandHandler> _logger;

        public ActivateBannerEventCommandHandler(
            IUnitOfWork unitOfWork,
            IProductPricingService pricingService,
            ILogger<ActivateBannerEventCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _pricingService = pricingService;
            _logger = logger;
        }

        public async Task<Result<BannerEventSpecialDTO>> Handle(ActivateBannerEventCommand request, CancellationToken cancellationToken)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var bannerEvent = await _unitOfWork.BannerEventSpecials.GetByIdAsync(request.BannerEventId, cancellationToken);
                    if (bannerEvent == null)
                    {
                        return Result<BannerEventSpecialDTO>.Failure($"Banner event with ID {request.BannerEventId} not found");
                    }

                    if (request.IsActive)
                    {
                        //  ACTIVATE EVENT
                        bannerEvent.IsActive = true;
                        bannerEvent.Status = EventStatus.Active;
                        bannerEvent.UpdatedAt = DateTime.UtcNow;

                        _logger.LogInformation("🟢 Activating banner event: {EventId} - {EventName}",
                            bannerEvent.Id, bannerEvent.Name);

                        // STEP 1: Update the event in database FIRST
                        await _unitOfWork.BannerEventSpecials.UpdateAsync(bannerEvent, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken); 

                        // STEP 2: Clear cache to ensure fresh pricing calculations
                        await _pricingService.InvalidateAllPriceCacheAsync();

                        // STEP 3: FORCE PRICE RECALCULATION for affected products
                        await ForceRecalculatePricesAsync(bannerEvent, cancellationToken);

                        _logger.LogInformation(" Banner event activated successfully: {EventName}. Product prices updated.",
                            bannerEvent.Name);

                        return Result<BannerEventSpecialDTO>.Success(
                            bannerEvent.ToDTO(),
                            $"Banner event '{bannerEvent.Name}' activated successfully. Product prices updated in real-time.");
                    }
                    else
                    {
                        //  DEACTIVATE EVENT
                        bannerEvent.IsActive = false;
                        bannerEvent.Status = EventStatus.Paused;
                        bannerEvent.UpdatedAt = DateTime.UtcNow;

                        _logger.LogInformation(" Deactivating banner event: {EventId} - {EventName}",
                            bannerEvent.Id, bannerEvent.Name);

                        // STEP 1: Update the event in database FIRST
                        await _unitOfWork.BannerEventSpecials.UpdateAsync(bannerEvent, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken); 

                        // STEP 2: Clear cache to revert to original prices
                        await _pricingService.InvalidateAllPriceCacheAsync();

                        _logger.LogInformation("Banner event deactivated successfully: {EventName}. Product prices reverted.",
                            bannerEvent.Name);

                        return Result<BannerEventSpecialDTO>.Success(
                            bannerEvent.ToDTO(),
                            $"Banner event '{bannerEvent.Name}' deactivated successfully. Product prices reverted to original.");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Failed to {Action} banner event {EventId}",
                    request.IsActive ? "activate" : "deactivate", request.BannerEventId);

                return Result<BannerEventSpecialDTO>.Failure(
                    $"Failed to {(request.IsActive ? "activate" : "deactivate")} banner event: {ex.Message}");
            }
        }

        //  Force recalculate prices for affected products
        private async Task ForceRecalculatePricesAsync(BannerEventSpecial bannerEvent, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(" Force recalculating prices for event {EventId}", bannerEvent.Id);

                // Get affected products
                List<int> affectedProductIds;

                if (bannerEvent.EventProducts?.Any() == true)
                {
                    // Specific products
                    affectedProductIds = bannerEvent.EventProducts.Select(ep => ep.ProductId).ToList();
                    _logger.LogInformation("Found {Count} specific products for event {EventId}",
                        affectedProductIds.Count, bannerEvent.Id);
                }
                else
                {
                    // Global event - get sample products to test (limit for performance)
                    var sampleProducts = await _unitOfWork.Products.GetAllAsync(
                        predicate: p => !p.IsDeleted,
                        take: 10, // Test with first 10 products
                        cancellationToken: cancellationToken);

                    affectedProductIds = sampleProducts.Select(p => p.Id).ToList();
                    _logger.LogInformation("Global event - testing with {Count} sample products",
                        affectedProductIds.Count);
                }

                // FORCE calculation by calling GetEffectivePriceAsync for each product
                foreach (var productId in affectedProductIds)
                {
                    try
                    {
                        var priceInfo = await _pricingService.GetEffectivePriceAsync(productId, null, cancellationToken);

                        _logger.LogDebug("Product {ProductId}: {OriginalPrice} → {EffectivePrice} (Discount: {HasDiscount})",
                            productId, priceInfo.OriginalPrice, priceInfo.EffectivePrice, priceInfo.HasDiscount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to calculate price for product {ProductId}", productId);
                    }
                }

                _logger.LogInformation("Completed price recalculation for event {EventId}", bannerEvent.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during price recalculation for event {EventId}", bannerEvent.Id);
            }
        }
    }
}