using Application.Features.ShippingFeat.Commands;
using FluentValidation;

namespace Application.Features.ShippingFeat.Validators
{
    public class CalculateShippingCommandValidator : AbstractValidator<CalculateShippingCommand>
    {
        public CalculateShippingCommandValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("User ID is required");

            RuleFor(x => x.OrderTotal)
                .GreaterThan(0).WithMessage("Order total must be greater than 0")
                .LessThanOrEqualTo(1000000).WithMessage("Order total cannot exceed ₨1,000,000");

            RuleFor(x => x.DeliveryLatitude)
                .Must(lat => lat == null || (lat >= -90 && lat <= 90))
                .WithMessage("Delivery latitude must be between -90 and 90 degrees if provided");

            RuleFor(x => x.DeliveryLongitude)
                .Must(lng => lng == null || (lng >= -180 && lng <= 180))
                .WithMessage("Delivery longitude must be between -180 and 180 degrees if provided");

            // If latitude is provided, longitude must also be provided
            RuleFor(x => x.DeliveryLongitude)
                .NotNull()
                .When(x => x.DeliveryLatitude.HasValue)
                .WithMessage("Delivery longitude is required when latitude is provided");

            RuleFor(x => x.DeliveryLatitude)
                .NotNull()
                .When(x => x.DeliveryLongitude.HasValue)
                .WithMessage("Delivery latitude is required when longitude is provided");

            RuleFor(x => x.RequestedDeliveryDate)
                .Must(date => date == null || date > DateTime.UtcNow)
                .WithMessage("Requested delivery date must be in the future if provided");

            RuleFor(x => x.PreferredConfigurationId)
                .GreaterThan(0)
                .When(x => x.PreferredConfigurationId.HasValue)
                .WithMessage("Preferred configuration ID must be greater than 0 if provided");
        }
    }
}