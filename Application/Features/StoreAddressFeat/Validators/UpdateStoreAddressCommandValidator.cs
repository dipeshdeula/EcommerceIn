using Application.Common.Helper;
using Application.Features.StoreAddressFeat.Commands;
using Application.Interfaces.Repositories;
using FluentValidation;

namespace Application.Features.StoreAddressFeat.Validators
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

            When(x => x.updateStoreAddressDTO.Street is not null, () =>
            {
                RuleFor(x => x.updateStoreAddressDTO.Street)
                    .NotEmpty().WithMessage("Street cannot be empty.")
                    .When(x => x.updateStoreAddressDTO.Street is not null);

            });

            When(x => x.updateStoreAddressDTO.City is not null, () =>
            {
                RuleFor(x => x.updateStoreAddressDTO.City)
                    .NotEmpty().WithMessage("City cannot be empty.")
                    .Must(DataHelper.IsValidCity).WithMessage("Invalid Province City")
                    .When(x => x.updateStoreAddressDTO.City is not null);
            });

            When(x => x.updateStoreAddressDTO.Province is not null, () =>
            {
                RuleFor(x => x.updateStoreAddressDTO.Province)
                    .NotEmpty().WithMessage("Province cannot be empty.")
                    .Must(DataHelper.IsValidProvince).WithMessage("Invalid Province name")
                    .When(x => x.updateStoreAddressDTO.Province is not null);

            });

            When(x => x.updateStoreAddressDTO.PostalCode is not null, () =>
            {
                RuleFor(x => x.updateStoreAddressDTO.PostalCode)
                    .NotEmpty().WithMessage("PostalCode cannot be empty.")
                    .When(x => x.updateStoreAddressDTO.PostalCode is not null);

            });

            When(x => x.updateStoreAddressDTO.Latitude.HasValue, () =>
            {
                RuleFor(x => x.updateStoreAddressDTO.Latitude.Value)
                    .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.")
                    .When(x => x.updateStoreAddressDTO.Latitude is not null);

            });

            When(x => x.updateStoreAddressDTO.Longitude.HasValue, () =>
            {
                RuleFor(x => x.updateStoreAddressDTO.Longitude.Value)
                    .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.")
                    .When(x => x.updateStoreAddressDTO.Longitude is not null);

            });
        }
    }
}
