using Application.Dto.LocationDTOs;
using Application.Interfaces.Repositories;
using FluentValidation;

namespace Application.Features.LocationFeat.Validators
{
    public class ServiceAreaValidator : AbstractValidator<AddServiceAreaDTO>
    {
        private readonly IServiceAreaRepository _serviceAreaRepository;
        public ServiceAreaValidator(IServiceAreaRepository serviceAreaRepository)
        {
            _serviceAreaRepository = serviceAreaRepository;

            RuleFor(x => x.CityName)
                .NotEmpty().WithMessage("City name is required")
                .MaximumLength(100);

            RuleFor(x => x.Province)
                .NotEmpty().WithMessage("Province is required")
                .MaximumLength(100);

            RuleFor(x => x.Country)
                .NotEmpty().WithMessage("Country is required")
                .MaximumLength(100);

            RuleFor(x => x.CenterLatitude)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90");

            RuleFor(x => x.CenterLongitude)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180");

            RuleFor(x => x.RadiusKm)
                .GreaterThan(0).WithMessage("Radius must be greater than 0");

            RuleFor(x => x.MaxDeliveryDistancekm)
                .GreaterThan(0).WithMessage("Max delivery distance must be greater than 0");

            RuleFor(x => x.MinOrderAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Min order amount cannot be negative");

            RuleFor(x => x.DeliveryStartTime)
                .LessThan(x => x.DeliveryEndTime)
                .WithMessage("Delivery start time must be before end time");

            RuleFor(x => x.EstimatedDeliveryDays)
                .GreaterThanOrEqualTo(0).WithMessage("Estimated delivery days cannot be negative");

            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Display name is required")
                .MaximumLength(100);

            RuleFor(x => x.Description)
                .MaximumLength(500);

            RuleFor(x => x.NotAvailableMessage)
                .MaximumLength(200);
        }
    }

    public class UpdateServiceAreaValidator : AbstractValidator<UpdateServiceAreaDTO>
    {
        private readonly IServiceAreaRepository _serviceAreaRepository;

        public UpdateServiceAreaValidator(IServiceAreaRepository serviceAreaRepository)
        {
            _serviceAreaRepository = serviceAreaRepository;
            RuleFor(x => x.CityName)
                .MaximumLength(100);

            RuleFor(x => x.Province)
                .MaximumLength(100);

            RuleFor(x => x.Country)
                .MaximumLength(100);

            // Only use .HasValue for nullable types (e.g., double?, int?)
            RuleFor(x => x.CenterLatitude)
                .InclusiveBetween(-90, 90)
                .When(x => x.CenterLatitude != null);

            RuleFor(x => x.CenterLongitude)
                .InclusiveBetween(-180, 180)
                .When(x => x.CenterLongitude != null);

            RuleFor(x => x.RadiusKm)
                .GreaterThan(0)
                .When(x => x.RadiusKm != null);

            RuleFor(x => x.MaxDeliveryDistancekm)
                .GreaterThan(0)
                .When(x => x.MaxDeliveryDistancekm != null);

            RuleFor(x => x.MinOrderAmount)
                .GreaterThanOrEqualTo(0)
                .When(x => x.MinOrderAmount != null);

            RuleFor(x => x.DeliveryStartTime)
                .LessThan(x => x.DeliveryEndTime ?? default)
                .When(x => x.DeliveryStartTime != null && x.DeliveryEndTime != null);

            RuleFor(x => x.EstimatedDeliveryDays)
                .GreaterThanOrEqualTo(0)
                .When(x => x.EstimatedDeliveryDays != null);

            RuleFor(x => x.DisplayName)
                .MaximumLength(100);

            RuleFor(x => x.Description)
                .MaximumLength(500);

            RuleFor(x => x.NotAvailableMessage)
                .MaximumLength(200);
        }
    }
}