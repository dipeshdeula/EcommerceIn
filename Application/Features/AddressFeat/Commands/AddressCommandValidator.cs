using Application.Interfaces.Repositories;
using FluentValidation;
using System.Globalization;

namespace Application.Features.AddressFeat.Commands
{
    public class AddressCommandValidator : AbstractValidator<AddressCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IAddressRepository _addressRepository;
        public AddressCommandValidator(IUserRepository userRepository, IAddressRepository addressRepository)
        {

            string[] Provinces = { "Koshi", "Madhesh", "Bagmati", "Gandaki", "Lumbini", "Karnali", "Sudurpashchim" };
            _userRepository = userRepository;
            _addressRepository = addressRepository;


            RuleFor(x => x.Label)
                .NotEmpty().WithMessage("Label is required.")
                .MaximumLength(50).WithMessage("Label must not exceed 50 characters.");

            RuleFor(x => x.Street)
                .NotEmpty().WithMessage("Street is required.")
                .MaximumLength(100).WithMessage("Street must not exceed 100 characters.");
            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required.")
                .MaximumLength(50).WithMessage("City must not exceed 50 characters.");
            RuleFor(x => x.Province)
                .NotEmpty().WithMessage("Province is required.")
                .Must(p => Provinces.Contains(p)).WithMessage($"Province must be one of the following: {string.Join(", ", Provinces)}.");
            RuleFor(x => x.PostalCode)
                .NotEmpty().WithMessage("Postal code is required.")
                .Matches(@"^\d{5}$").WithMessage("Postal code must be a 5-digit number.");
            RuleFor(x => x.Latitude)
            .NotEmpty().WithMessage("Latitude is required.")
            .Must(lat =>
            {
                if (!double.TryParse(lat, NumberStyles.Any, CultureInfo.InvariantCulture, out double latitude))
                    return false;
                return latitude >= -90 && latitude <= 90;
            })
            .WithMessage("Latitude must be a valid number between -90 and 90.");

            RuleFor(x => x.Longitude)
                .NotEmpty().WithMessage("Longitude is required.")
                .Must(lon =>
                {
                    if (!double.TryParse(lon, NumberStyles.Any, CultureInfo.InvariantCulture, out double longitude))
                        return false;
                    return longitude >= -180 && longitude <= 180;
                })
                .WithMessage("Longitude must be a valid number between -180 and 180.");
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("User ID is required.")
                .MustAsync(async (id, cancellation) => await _userRepository.AnyAsync(c => c.Id == id))
                .WithMessage("User doesn't exist!");
        }
    }
}
