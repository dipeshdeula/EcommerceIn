using Application.Interfaces.Repositories;
using FluentValidation;

namespace Application.Features.ProductFeat.Commands
{
    public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
    {
        public CreateProductCommandValidator(ISubSubCategoryRepository subSubCategoryRepository)
        {
            RuleFor(x => x.SubSubCategoryId)
                .GreaterThan(0).WithMessage("SubSubCategoryId must be greater than 0.")
                .MustAsync(async (id, _) =>
                    await subSubCategoryRepository.AnyAsync(s => s.Id == id))
                .WithMessage("SubSubCategory does not exist.");

            RuleFor(x => x.createProductDTO)
                .NotNull().WithMessage("Product data is required.")
                .SetValidator(new CreateProductDTOValidator());
        }
    }
}
