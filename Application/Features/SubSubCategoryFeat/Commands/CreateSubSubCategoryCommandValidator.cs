using Application.Interfaces.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.SubSubCategoryFeat.Commands
{
    public class CreateSubSubCategoryCommandValidator : AbstractValidator<CreateSubSubCategoryCommand>
    {
        public CreateSubSubCategoryCommandValidator(ISubCategoryRepository subCategoryRepository)
        {
            RuleFor(x => x.SubCategoryId)
                .GreaterThan(0).WithMessage("SubCategoryId must be greater than 0.")
                .MustAsync(async (id, _) => await subCategoryRepository.AnyAsync(sc => sc.Id == id))
                .WithMessage("SubCategory does not exist.");

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
