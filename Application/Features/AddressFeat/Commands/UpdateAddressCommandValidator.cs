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
            string[] Cities = {
            "Kathmandu","Lalitpur","Bhaktapur","Pokhara","Biratnagar","Birgunj","Butwal","Dharan","Janakpur","Hetauda","Nepalgunj","Dhangadhi",
            "Itahari","Bharatpur","Tulsipur","Ghorahi","Bhimdatta (Mahendranagar)","Kirtipur","Tikapur","Rajbiraj","Gaur", "Kalaiya", "Siraha",
            "Inaruwa","Lahan","Panauti","Banepa","Dhankuta","Bardibas","Besisahar","Sandhikharka","Chainpur","Jaleshwor","Gaighat", "Damauli","Tansen",
            "Baglung","Amargadhi","Waling","Beni","Dipayal","Bhairahawa","Simara","Chandrapur","Lamahi","Bardiya","Parasi","Phidim","Ilam","Putalibazar",
            "Rampur","Melamchi","Khairahani","Madhyapur Thimi","Tokha","Kohalpur","Shivraj","Sunwal","Chautara","Suryabinayak","Godawari (Lalitpur)","Godawari (Kailali)",
            "Barahathawa","Bardaghat","Manma","Martadi","Charikot","Rukumkot","Jiri","Bajura","Diktel","Tumlingtar","Salleri"};


            RuleFor(x => x.id)
                .NotEmpty().WithMessage("Address ID is required.")
                .MustAsync(async (id, cancellation) => await _addressRepository.AnyAsync(a => a.Id == id))
                .WithMessage("Address doesn't exist!");

            RuleFor(x => x.updateAddressDto.Label)
                .MaximumLength(50).WithMessage("Label must not exceed 50 characters.")
                .When(x => x.updateAddressDto.Label is not null);

            RuleFor(x => x.updateAddressDto.Street)
                .MaximumLength(100).WithMessage("Street must not exceed 100 characters.")
                .When(x => x.updateAddressDto.Street is not null);

            RuleFor(x => x.updateAddressDto.City)
                .Must(c=>Cities.Contains(c!))
                .MaximumLength(50).WithMessage("City must not exceed 50 characters.")
                .WithMessage($"City must be one of the following :{string.Join(", ", Cities)}.")
                .When(x => x.updateAddressDto.City is not null);

            RuleFor(x => x.updateAddressDto.Province)
                .Must(p => Provinces.Contains(p!))
                .WithMessage($"Province must be one of the following: {string.Join(", ", Provinces)}.")
                .When(x => x.updateAddressDto.Province is not null);

            RuleFor(x => x.updateAddressDto.PostalCode)
                .Matches(@"^\d{5}$").WithMessage("Postal code must be a 5-digit number.")
                .When(x => x.updateAddressDto.PostalCode is not null);

            RuleFor(x => x.updateAddressDto.Latitude.ToString())
                .Matches(@"^[-+]?(90(\.0+)?|([1-8]?\d)(\.\d+)?)$")
                .WithMessage("Latitude must be a valid number between -90 and 90.")
                .When(x => x.updateAddressDto.Latitude is not null);

            RuleFor(x => x.updateAddressDto.Longitude.ToString())
                .Matches(@"^[-+]?(180(\.0+)?|((1[0-7]\d)|([1-9]?\d))(\.\d+)?)$")
                .WithMessage("Longitude must be a valid number between -180 and 180.")
                .When(x => x.updateAddressDto.Longitude is not null);
        }
    }
}
