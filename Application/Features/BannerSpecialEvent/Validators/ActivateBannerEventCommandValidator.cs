/*using Application.Interfaces.Services;
using Domain.Enums.BannerEventSpecial;
using FluentValidation;

namespace Application.Features.BannerSpecialEvent.Commands
{
    public class ActivateBannerEventCommandValidator : AbstractValidator<ActivateBannerEventCommand>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ActivateBannerEventCommandValidator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            // ✅ Basic validation
            RuleFor(x => x.BannerEventId)
                .GreaterThan(0)
                .WithMessage("Banner event ID must be greater than 0")
                .MustAsync(EventExists)
                .WithMessage("Banner event does not exist or has been deleted");

            // ✅ Activation validation
            RuleFor(x => x)
                .MustAsync(EventCanBeActivated)
                .When(x => x.IsActive)
                .WithMessage("Event cannot be activated. Check date range, usage limits, and current status.");

            // ✅ Deactivation validation  
            RuleFor(x => x)
                .MustAsync(EventCanBeDeactivated)
                .When(x => !x.IsActive)
                .WithMessage("Event cannot be deactivated. Event is not currently active.");

            // ✅ Conflict validation for activation
            RuleFor(x => x)
                .MustAsync(NoConflictingHigherPriorityEvents)
                .When(x => x.IsActive)
                .WithMessage("Cannot activate event due to conflicting higher priority events");

            // ✅ Product validation for activation
            RuleFor(x => x)
                .MustAsync(HasValidProducts)
                .When(x => x.IsActive)
                .WithMessage("Event has no valid/active products associated with it");
        }

        // ✅ Check if event exists and is not deleted
        private async Task<bool> EventExists(int eventId, CancellationToken cancellationToken)
        {
            var bannerEvent = await _unitOfWork.BannerEventSpecials.FindByIdAsync(eventId);
            return bannerEvent != null && !bannerEvent.IsDeleted;
        }

        // ✅ Comprehensive activation validation
        private async Task<bool> EventCanBeActivated(ActivateBannerEventCommand command, CancellationToken cancellationToken)
        {
            var bannerEvent = await _unitOfWork.BannerEventSpecials.FindByIdAsync(command.BannerEventId);
            if (bannerEvent == null || bannerEvent.IsDeleted)
                return false;

            var now = DateTime.UtcNow;

            // Check if already active
            if (bannerEvent.IsActive)
                return false;

            // Check date range
            if (bannerEvent.StartDate > now)
                return false; // Cannot activate before start date

            if (bannerEvent.EndDate < now)
                return false; // Cannot activate after end date

            // Check usage limits
            if (bannerEvent.CurrentUsageCount >= bannerEvent.MaxUsageCount)
                return false;

            // Check status
            if (bannerEvent.Status == EventStatus.Expired || bannerEvent.Status == EventStatus.Cancelled)
                return false;

            return true;
        }

        // ✅ Deactivation validation
        private async Task<bool> EventCanBeDeactivated(ActivateBannerEventCommand command, CancellationToken cancellationToken)
        {
            var bannerEvent = await _unitOfWork.BannerEventSpecials.FindByIdAsync(command.BannerEventId);
            if (bannerEvent == null || bannerEvent.IsDeleted)
                return false;

            // Check if already inactive
            if (!bannerEvent.IsActive)
                return false;

            return true;
        }

        // ✅ Check for conflicting higher priority events
        private async Task<bool> NoConflictingHigherPriorityEvents(ActivateBannerEventCommand command, CancellationToken cancellationToken)
        {
            var bannerEvent = await _unitOfWork.BannerEventSpecials.FindByIdAsync(command.BannerEventId);
            if (bannerEvent == null)
                return false;

            // Get overlapping events with higher priority
            var conflictingEvents = await _unitOfWork.BannerEventSpecials.GetAllAsync(
                predicate: e => e.Id != bannerEvent.Id &&
                            e.IsActive &&
                            !e.IsDeleted &&
                            e.Status == EventStatus.Active &&
                            e.Priority > bannerEvent.Priority &&
                            e.StartDate <= bannerEvent.EndDate &&
                            e.EndDate >= bannerEvent.StartDate,
                includeDeleted: false);

            return !conflictingEvents.Any();
        }

        // ✅ Check if event has valid products
        private async Task<bool> HasValidProducts(ActivateBannerEventCommand command, CancellationToken cancellationToken)
        {
            var eventProducts = await _unitOfWork.EventProducts.GetAllAsync(
                predicate: ep => ep.BannerEventId == command.BannerEventId,
                includeDeleted: false);

            // If no specific products, it's a general event (valid)
            if (!eventProducts.Any())
                return true;

            // Check if associated products are still active
            var productIds = eventProducts.Select(ep => ep.ProductId).ToList();
            var activeProducts = await _unitOfWork.Products.GetAllAsync(
                predicate: p => productIds.Contains(p.Id) && !p.IsDeleted,
                includeDeleted: false);

            return activeProducts.Any();
        }
    }
}*/