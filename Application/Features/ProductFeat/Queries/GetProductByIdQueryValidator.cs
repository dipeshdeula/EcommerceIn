using Application.Interfaces.Repositories;
using FluentValidation;

namespace Application.Features.ProductFeat.Queries
{
    public class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery>
    {
        public GetProductByIdQueryValidator(IProductRepository productRepository)
        {
            RuleFor(x => x.productId)
                .GreaterThan(0).WithMessage("ProductId must be greater than 0.")
                .MustAsync(async (id, _) =>
                    await productRepository.AnyAsync(p => p.Id == id))
                .WithMessage("Product not found.");

            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("Page number must be greater than 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than 0.");
        }
    }
}
