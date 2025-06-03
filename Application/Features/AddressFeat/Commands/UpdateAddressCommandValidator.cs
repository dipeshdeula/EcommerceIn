using Application.Interfaces.Repositories;
using FluentValidation;

namespace Application.Features.AddressFeat.Commands
{
    public class UpdateAddressCommandValidator : AbstractValidator<UpdateAddressCommand>
    {
        private readonly IAddressRepository _addressRepository;

        public UpdateAddressCommandValidator(IAddressRepository addressRepository)
        {
            _addressRepository = addressRepository;

            string[] Provinces = { "Koshi", "Madhesh", "Bagmati", "Gandaki", "Lumbini", "Karnali", "Sudurpashchim" };

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Address ID is required.")
                .MustAsync(async (id, cancellation) => await _addressRepository.AnyAsync(a => a.Id == id))
                .WithMessage("Address doesn't exist!");

            RuleFor(x => x.Label)
                .MaximumLength(50).WithMessage("Label must not exceed 50 characters.")
                .When(x => x.Label is not null);

            RuleFor(x => x.Street)
                .MaximumLength(100).WithMessage("Street must not exceed 100 characters.")
                .When(x => x.Street is not null);

            RuleFor(x => x.City)
                .MaximumLength(50).WithMessage("City must not exceed 50 characters.")
                .When(x => x.City is not null);

            RuleFor(x => x.Province)
                .Must(p => Provinces.Contains(p!))
                .WithMessage($"Province must be one of the following: {string.Join(", ", Provinces)}.")
                .When(x => x.Province is not null);

            RuleFor(x => x.PostalCode)
                .Matches(@"^\d{5}$").WithMessage("Postal code must be a 5-digit number.")
                .When(x => x.PostalCode is not null);

            RuleFor(x => x.Latitude.ToString())
                .Matches(@"^[-+]?(90(\.0+)?|([1-8]?\d)(\.\d+)?)$")
                .WithMessage("Latitude must be a valid number between -90 and 90.")
                .When(x => x.Latitude is not null);

            RuleFor(x => x.Longitude.ToString())
                .Matches(@"^[-+]?(180(\.0+)?|((1[0-7]\d)|([1-9]?\d))(\.\d+)?)$")
                .WithMessage("Longitude must be a valid number between -180 and 180.")
                .When(x => x.Longitude is not null);
        }
    }
}
