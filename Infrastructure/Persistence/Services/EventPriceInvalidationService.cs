using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Enums.BannerEventSpecial;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services
{
    public class EventPriceInvalidationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventPriceInvalidationService> _logger;

        public EventPriceInvalidationService(
            IServiceProvider serviceProvider,
            ILogger<EventPriceInvalidationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event Price Invalidation Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var pricingService = scope.ServiceProvider.GetRequiredService<IProductPricingService>();

                    // 1. Check for expired events and update their status
                    var expiredEvents = await unitOfWork.BannerEventSpecials.GetExpiredEventsAsync(stoppingToken);

                    foreach (var expiredEvent in expiredEvents)
                    {
                        _logger.LogInformation("Processing expired event: {EventName} (ID: {EventId})",
                            expiredEvent.Name, expiredEvent.Id);

                        // Update event status
                        expiredEvent.Status = EventStatus.Expired;
                        expiredEvent.IsActive = false;
                        expiredEvent.UpdatedAt = DateTime.UtcNow;

                        await unitOfWork.BannerEventSpecials.UpdateAsync(expiredEvent, stoppingToken);

                        //  2. Invalidate price cache for affected products
                        if (expiredEvent.EventProducts?.Any() == true)
                        {
                            foreach (var eventProduct in expiredEvent.EventProducts)
                            {
                                await pricingService.InvalidatePriceCacheAsync(eventProduct.ProductId);
                            }
                            _logger.LogInformation("Invalidated price cache for {ProductCount} specific products",
                                expiredEvent.EventProducts.Count);
                        }
                        else
                        {
                            // Global event - invalidate all product pricing
                            await pricingService.InvalidateAllPriceCacheAsync();
                            _logger.LogInformation("Invalidated all product pricing cache (global event)");
                        }
                    }

                    //  3. Check for events that should start now
                    var now = DateTime.UtcNow;
                    var eventsToActivate = await unitOfWork.BannerEventSpecials.GetAllAsync(
                        predicate: e => !e.IsDeleted &&
                                       e.Status == EventStatus.Scheduled &&
                                       e.StartDate <= now &&
                                       e.EndDate >= now,
                        cancellationToken: stoppingToken);

                    foreach (var eventToActivate in eventsToActivate)
                    {
                        eventToActivate.Status = EventStatus.Active;
                        eventToActivate.IsActive = true;
                        eventToActivate.UpdatedAt = DateTime.UtcNow;

                        await unitOfWork.BannerEventSpecials.UpdateAsync(eventToActivate, stoppingToken);
                        await pricingService.RefreshPricesForEventAsync(eventToActivate.Id, stoppingToken);

                        _logger.LogInformation("Activated scheduled event: {EventName} (ID: {EventId})",
                            eventToActivate.Name, eventToActivate.Id);
                    }

                    // 4. Save all changes
                    if (expiredEvents.Any() || eventsToActivate.Any())
                    {
                        await unitOfWork.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Processed {ExpiredCount} expired events and {ActivatedCount} new events",
                            expiredEvents.Count(), eventsToActivate.Count());
                    }

                    // 5. Run every 2 minutes for real-time responsiveness
                    await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in event price invalidation service");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Shorter delay on error
                }
            }
        }
    }
}