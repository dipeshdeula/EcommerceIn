using Application.Interfaces.Repositories;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.OrderFeat.Commands
{
    public class CreatePlaceOrderCommandValidator : AbstractValidator<CreatePlaceOrderCommand>
    {
        private readonly IUserRepository _userRepository;

        public CreatePlaceOrderCommandValidator(IUserRepository userRepository)
        {
            _userRepository = userRepository;

            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("UserId must be greater than 0.")
                .MustAsync(async (userId, _) =>
                    await _userRepository.AnyAsync(u => u.Id == userId))
                .WithMessage("User does not exist.");

            RuleFor(x => x.ShippingAddress)
                .NotEmpty().WithMessage("Shipping address is required.")
                .MaximumLength(250).WithMessage("Shipping address must be 250 characters or fewer.");

            RuleFor(x => x.ShippingCity)
                .NotEmpty().WithMessage("Shipping city is required.")
                .MaximumLength(100).WithMessage("Shipping city must be 100 characters or fewer.");
        }
    }
}
