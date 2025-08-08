using Application.Features.ShippingFeat.Commands;
using FluentValidation;

namespace Application.Features.ShippingFeat.Validators
{
    public class UpdateShippingCommandValidator : AbstractValidator<UpdateShippingCommand>
    {
        public UpdateShippingCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Shipping configuration ID is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Configuration name is required")
                .Length(2, 100).WithMessage("Configuration name must be between 2 and 100 characters");

            RuleFor(x => x.LowOrderThreshold)
                .GreaterThanOrEqualTo(0).WithMessage("Low order threshold must be 0 or greater")
                .LessThan(x => x.FreeShippingThreshold).WithMessage("Low order threshold must be less than free shipping threshold");

            RuleFor(x => x.LowOrderShippingCost)
                .GreaterThanOrEqualTo(0).WithMessage("Low order shipping cost must be 0 or greater");

            RuleFor(x => x.HighOrderShippingCost)
                .GreaterThanOrEqualTo(0).WithMessage("High order shipping cost must be 0 or greater");

            RuleFor(x => x.FreeShippingThreshold)
                .GreaterThan(0).WithMessage("Free shipping threshold must be greater than 0")
                .GreaterThan(x => x.LowOrderThreshold).WithMessage("Free shipping threshold must be greater than low order threshold");

            RuleFor(x => x.EstimatedDeliveryDays)
                .GreaterThan(0).WithMessage("Estimated delivery days must be greater than 0")
                .LessThanOrEqualTo(30).WithMessage("Estimated delivery days cannot exceed 30 days");

            RuleFor(x => x.MaxDeliveryDistanceKm)
                .GreaterThan(0).WithMessage("Maximum delivery distance must be greater than 0")
                .LessThanOrEqualTo(100).WithMessage("Maximum delivery distance cannot exceed 100 km");

            RuleFor(x => x.WeekendSurcharge)
                .GreaterThanOrEqualTo(0).WithMessage("Weekend surcharge must be 0 or greater");

            RuleFor(x => x.HolidaySurcharge)
                .GreaterThanOrEqualTo(0).WithMessage("Holiday surcharge must be 0 or greater");

            RuleFor(x => x.RushDeliverySurcharge)
                .GreaterThanOrEqualTo(0).WithMessage("Rush delivery surcharge must be 0 or greater");

            RuleFor(x => x.FreeShippingDescription)
                .MaximumLength(500).WithMessage("Free shipping description cannot exceed 500 characters");

            RuleFor(x => x.CustomerMessage)
                .MaximumLength(1000).WithMessage("Customer message cannot exceed 1000 characters");

            RuleFor(x => x.AdminNotes)
                .MaximumLength(2000).WithMessage("Admin notes cannot exceed 2000 characters");

            RuleFor(x => x.ModifiedByUserId)
                .GreaterThan(0).WithMessage("Modified by user ID is required");

            // Conditional validation for free shipping dates
            When(x => x.IsFreeShippingActive, () => {
                RuleFor(x => x.FreeShippingStartDate)
                    .NotNull().WithMessage("Free shipping start date is required when free shipping is active");

                RuleFor(x => x.FreeShippingEndDate)
                    .NotNull().WithMessage("Free shipping end date is required when free shipping is active")
                    .GreaterThan(x => x.FreeShippingStartDate).WithMessage("Free shipping end date must be after start date");
            });
        }
    }
}