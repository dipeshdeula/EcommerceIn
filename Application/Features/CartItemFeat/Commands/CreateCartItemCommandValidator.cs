using Application.Interfaces.Repositories;
using FluentValidation;

namespace Application.Features.CartItemFeat.Commands
{
    public class CreateCartItemCommandValidator : AbstractValidator<CreateCartItemCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IProductRepository _productRepository;

        public CreateCartItemCommandValidator(IUserRepository userRepository, IProductRepository productRepository)
        {
            _userRepository = userRepository;
            _productRepository = productRepository;

            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("UserId must be greater than 0.")
                .MustAsync(async (userId, cancellation) =>
                    await _userRepository.AnyAsync(u => u.Id == userId))
                .WithMessage("User does not exist.");

            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("ProductId must be greater than 0.")
                .MustAsync(async (productId, cancellation) =>
                    await _productRepository.AnyAsync(p => p.Id == productId))
                .WithMessage("Product does not exist.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0.")
                .MustAsync(async (command, quantity, cancellation) =>
                {
                    var product = await _productRepository.GetProductWithFullDetailsAsync(command.ProductId, cancellation);
                    return product != null && product.AvailableStock >= quantity;
                })
                .WithMessage("Insufficient product quantity available.");
        }
    }
}
