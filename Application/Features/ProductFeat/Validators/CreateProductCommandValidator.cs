using Application.Features.ProductFeat.Commands;
using Application.Interfaces.Repositories;
using FluentValidation;

namespace Application.Features.ProductFeat.Validators
{
    public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
    {
        public CreateProductCommandValidator(ICategoryRepository categoryRepository)
        {
            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("CategoryId must be greater than 0.")
                .MustAsync(async (id, _) =>
                    await categoryRepository.AnyAsync(c => c.Id == id))
                .WithMessage("Category does not exist.");

            RuleFor(x => x.createProductDTO)
                .NotNull().WithMessage("Product data is required.")
                .SetValidator(new CreateProductDTOValidator());

        }
    }
}
