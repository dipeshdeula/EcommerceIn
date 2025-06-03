using Application.Interfaces.Repositories;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.StoreAddressFeat.Commands
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

            RuleFor(x => x.Street)
                .NotEmpty().WithMessage("Street is required.");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required.");

            RuleFor(x => x.Province)
                .NotEmpty().WithMessage("Province is required.");

            RuleFor(x => x.PostalCode)
                .NotEmpty().WithMessage("PostalCode is required.");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.");
        }
    }

}
