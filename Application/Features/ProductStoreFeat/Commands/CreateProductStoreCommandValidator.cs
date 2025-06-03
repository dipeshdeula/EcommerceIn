using Application.Interfaces.Repositories;
using FluentValidation;

namespace Application.Features.ProductStoreFeat.Commands
{
    public class CreateProductStoreCommandValidator : AbstractValidator<CreateProductStoreCommand>
    {
        public CreateProductStoreCommandValidator(
            IStoreRepository storeRepository,
            IProductRepository productRepository)
        {
            RuleFor(x => x.StoreId)
                .GreaterThan(0).WithMessage("StoreId must be greater than 0.")
                .MustAsync(async (id, _) =>
                    await storeRepository.AnyAsync(s => s.Id == id))
                .WithMessage("Store does not exist.");

            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("ProductId must be greater than 0.")
                .MustAsync(async (id, _) =>
                    await productRepository.AnyAsync(p => p.Id == id))
                .WithMessage("Product does not exist.");
        }
    }
}
