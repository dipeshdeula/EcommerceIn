using Application.Interfaces.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.UploadImages
{
    public class UploadProductImagesCommandValidator : AbstractValidator<UploadProductImagesCommand>
    {
        private readonly IProductRepository _productRepository;

        public UploadProductImagesCommandValidator(IProductRepository productRepository)
        {
            _productRepository = productRepository;

            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("ProductId must be greater than 0.")
                .MustAsync(async (id, _) => await _productRepository.AnyAsync(p => p.Id == id))
                .WithMessage("Product does not exist.");

            RuleFor(x => x.Files)
                .NotEmpty().WithMessage("At least one image file is required.")
                .Must(files => files.All(f => BeAnImage(f))).WithMessage("Only image files (jpg, jpeg, png) are allowed.")
                .Must(files => files.Count <= 10).WithMessage("You can upload up to 10 images.")
                .Must(files => files.All(f => f.Length <= 5 * 1024 * 1024)) // 5MB max per file
                .WithMessage("Each image must be less than 5MB in size.");
        }

        private bool BeAnImage(IFormFile file)
        {
            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
            return allowedMimeTypes.Contains(file.ContentType.ToLower());
        }
    }
}
