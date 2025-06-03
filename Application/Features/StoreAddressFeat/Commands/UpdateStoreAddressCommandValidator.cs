using Application.Interfaces.Repositories;
using FluentValidation;

namespace Application.Features.StoreAddressFeat.Commands
{
    public class UpdateStoreAddressCommandValidator : AbstractValidator<UpdateStoreAddressCommand>
    {
        public UpdateStoreAddressCommandValidator(IStoreRepository storeRepository)
        {
            RuleFor(x => x.StoreId)
                .GreaterThan(0).WithMessage("StoreId must be greater than 0.")
                .MustAsync(async (id, _) =>
                    await storeRepository.AnyAsync(s => s.Id == id))
                .WithMessage("Store does not exist.");

            When(x => x.Street is not null, () =>
            {
                RuleFor(x => x.Street)
                    .NotEmpty().WithMessage("Street cannot be empty.");
            });

            When(x => x.City is not null, () =>
            {
                RuleFor(x => x.City)
                    .NotEmpty().WithMessage("City cannot be empty.");
            });

            When(x => x.Province is not null, () =>
            {
                RuleFor(x => x.Province)
                    .NotEmpty().WithMessage("Province cannot be empty.");
            });

            When(x => x.PostalCode is not null, () =>
            {
                RuleFor(x => x.PostalCode)
                    .NotEmpty().WithMessage("PostalCode cannot be empty.");
            });

            When(x => x.Latitude.HasValue, () =>
            {
                RuleFor(x => x.Latitude.Value)
                    .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.");
            });

            When(x => x.Longitude.HasValue, () =>
            {
                RuleFor(x => x.Longitude.Value)
                    .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.");
            });
        }
    }
}
