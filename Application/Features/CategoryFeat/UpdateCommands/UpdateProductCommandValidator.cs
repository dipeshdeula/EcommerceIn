using Application.Interfaces.Repositories;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.UpdateCommands
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

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.")
                .MustAsync(async (command, name, cancellation) =>
                    !await _productRepository.AnyAsync(p =>
                        p.Name == name && p.Id != command.ProductId))
                .WithMessage("Another product with the same name already exists.");

            RuleFor(x => x.Slug)
                .NotEmpty().WithMessage("Slug is required.")
                .MaximumLength(100).WithMessage("Slug cannot exceed 100 characters.")
                .MustAsync(async (command, slug, cancellation) =>
                    !await _productRepository.AnyAsync(p =>
                        p.Slug == slug && p.Id != command.ProductId))
                .WithMessage("Another product with the same slug already exists.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
        }
    }
}

