using Application.Dto;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.ProductFeat.Commands
{
    public class CreateProductDTOValidator : AbstractValidator<CreateProductDTO>
    {
        public CreateProductDTOValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required.")
                .MaximumLength(150).WithMessage("Name must be 150 characters or fewer.");

            RuleFor(x => x.Slug)
                .NotEmpty().WithMessage("Slug is required.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.");



            RuleFor(x => x.DiscountPercentage)
                .GreaterThanOrEqualTo(0).WithMessage("Discount percentage must be greater than 0")
                .LessThanOrEqualTo(100).WithMessage("Discount percentage must be less than or equal to 100");
                

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");

            RuleFor(x => x.Sku)
                .NotEmpty().WithMessage("SKU is required.");

            RuleFor(x => x.Weight)
                .NotEmpty().WithMessage("Weight is required.");

            RuleFor(x => x.Reviews)
                .GreaterThanOrEqualTo(0).WithMessage("Reviews count cannot be negative.");

            RuleFor(x => x.Rating)
                .InclusiveBetween(0, 5).WithMessage("Rating must be between 0 and 5.");

            RuleFor(x => x.Dimensions)
                .NotEmpty().WithMessage("Dimensions are required.");
        }
    }
}
