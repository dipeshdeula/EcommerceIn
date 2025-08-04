using Application.Features.ProductFeat.Commands;
using Application.Interfaces.Repositories;
using FluentValidation;

namespace Application.Features.ProductFeat.Validators
{
    public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
    {
        private readonly IProductRepository _productRepository;

        public UpdateProductCommandValidator(IProductRepository productRepository)
        {
            _productRepository = productRepository;

            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("ProductId must be greater than 0.")
                .MustAsync(async (id, cancellation) =>
                    await _productRepository.AnyAsync(p => p.Id == id))
                .WithMessage("Product does not exist.");

            RuleFor(x => x.updateProductDto.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");
                /*.MustAsync(async (command, name, cancellation) =>
                    !await _productRepository.AnyAsync(p =>
                        p.ProviderName.ToLower() == name.ToLower() && p.Id != command.ProductId))
                .WithMessage("Another product with the same name already exists.");*/

            RuleFor(x => x.updateProductDto.Slug)
                .NotEmpty().WithMessage("Slug is required.")
                .MaximumLength(100).WithMessage("Slug cannot exceed 100 characters.")
                .MustAsync(async (command, slug, cancellation) =>
                    !await _productRepository.AnyAsync(p =>
                        p.Slug == slug && p.Id != command.ProductId))
                .WithMessage("Another product with the same slug already exists.");

            RuleFor(x => x.updateProductDto.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");

            RuleFor(x => x.updateProductDto.DiscountPercentage)
               .GreaterThanOrEqualTo(0).WithMessage("Discount percentage must be greater than 0")
               .LessThanOrEqualTo(100).WithMessage("Discount percentage must be less than or equal to 100");

            RuleFor(x => x.updateProductDto.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");

            RuleFor(x => x.updateProductDto.Sku)
                .NotEmpty().WithMessage("SKU is required.");

            RuleFor(x => x.updateProductDto.Weight)
                .NotEmpty().WithMessage("Weight is required.");

            RuleFor(x => x.updateProductDto.Reviews)
                .GreaterThanOrEqualTo(0).WithMessage("Reviews count cannot be negative.");

            RuleFor(x => x.updateProductDto.Rating)
                .InclusiveBetween(0, 5).WithMessage("Rating must be between 0 and 5.");

            RuleFor(x => x.updateProductDto.Dimensions)
                .NotEmpty().WithMessage("Dimensions are required.");
        }
    }
}

