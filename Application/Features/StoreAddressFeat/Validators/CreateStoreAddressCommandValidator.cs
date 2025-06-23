using Application.Common.Helper;
using Application.Features.StoreAddressFeat.Commands;
using Application.Interfaces.Repositories;
using FluentValidation;

namespace Application.Features.StoreAddressFeat.Validators
{
    public class CreateStoreAddressCommandValidator : AbstractValidator<CreateStoreAddressCommand>
    {
        public CreateStoreAddressCommandValidator(IStoreRepository storeRepository)
        {
            RuleFor(x => x.StoreId)
                .GreaterThan(0).WithMessage("StoreId must be greater than 0.")
                .MustAsync(async (id, _) =>
                    await storeRepository.AnyAsync(s => s.Id == id))
                .WithMessage("Store does not exist.");

            RuleFor(x => x.addStoreAddressDto.Street)
                .NotEmpty().WithMessage("Street is required.");

            RuleFor(x => x.addStoreAddressDto.City)
                .NotEmpty().WithMessage("City is required.")
                .Must(DataHelper.IsValidCity).WithMessage("Invalid city name");                

            RuleFor(x => x.addStoreAddressDto.Province)
                .NotEmpty().WithMessage("Province is required.")
                .Must(DataHelper.IsValidProvince)
                .WithMessage("Invalid province name");

            RuleFor(x => x.addStoreAddressDto.PostalCode)
                .NotEmpty().WithMessage("PostalCode is required.")
                .Matches(@"^\d{5}$").WithMessage("Postal code must be a 5-digit number.");


            RuleFor(x => x.addStoreAddressDto.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.");

            RuleFor(x => x.addStoreAddressDto.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.");
        }
    }

}
