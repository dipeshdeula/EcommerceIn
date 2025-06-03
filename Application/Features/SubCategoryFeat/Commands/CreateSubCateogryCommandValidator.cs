using Application.Interfaces.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.SubCategoryFeat.Commands
{
    public class CreateSubCategoryCommandValidator : AbstractValidator<CreateSubCategoryCommand>
    {
        public CreateSubCategoryCommandValidator(ICategoryRepository categoryRepository)
        {
            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("CategoryId must be greater than 0.")
                .MustAsync(async (id, _) => await categoryRepository.AnyAsync(c => c.Id == id))
                .WithMessage("Category does not exist.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.");

            RuleFor(x => x.Slug)
                .NotEmpty().WithMessage("Slug is required.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.");

            RuleFor(x => x.File)
                .NotNull().WithMessage("Image file is required.")
                .Must(BeAValidImage).WithMessage("Only image files (jpg, jpeg, png) are allowed.");
        }

        private bool BeAValidImage(IFormFile file)
        {
            if (file == null) return false;

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
            return allowedTypes.Contains(file.ContentType.ToLower());
        }
    }
}
