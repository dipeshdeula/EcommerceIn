using Application.Interfaces.Repositories;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CartItemFeat.Commands
{
    public class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IProductRepository _productRepository;

        public UpdateCartItemCommandValidator(IUserRepository userRepository, IProductRepository productRepository)
        {
            _userRepository = userRepository;
            _productRepository = productRepository;

            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Cart item ID must be greater than 0.");

            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("UserId must be greater than 0.")
                .MustAsync(async (userId, cancellation) =>
                    await _userRepository.AnyAsync(u => u.Id == userId))
                .WithMessage("User does not exist.");

            When(x => x.ProductId.HasValue, () =>
            {
                RuleFor(x => x.ProductId.Value)
                    .GreaterThan(0).WithMessage("ProductId must be greater than 0.")
                    .MustAsync(async (productId, cancellation) =>
                        await _productRepository.AnyAsync(p => p.Id == productId))
                    .WithMessage("Product does not exist.");
            });

            When(x => x.Quantity.HasValue, () =>
            {
                RuleFor(x => x.Quantity.Value)
                    .GreaterThan(0).WithMessage("Quantity must be greater than 0.");
            });
        }
    }
}
