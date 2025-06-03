using Application.Interfaces.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.Commands
{
    public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
    {
        private readonly ICategoryRepository _categoryRepository;
        public CreateCategoryCommandValidator(ICategoryRepository categoryRepository)
        {


            _categoryRepository = categoryRepository;

            RuleFor(x => x.File)
                    .NotNull().WithMessage("File is required.") // Ensure the file is not null
                    .Must(BeAnImage).WithMessage("Only image files (jpg, png, jpeg) are allowed.");




            RuleFor(x => x.Name)
               .MustAsync(async (name, cancellation) => !await _categoryRepository.AnyAsync(c => c.Name == name))
               .WithMessage("Category name already exists.");



            RuleFor(x => x.Slug).NotEmpty().WithMessage("Slug is Required.");

            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is Required.");




        }

        private bool BeAnImage(IFormFile file)
        {
            if (file == null)
                return false;

            // Allowed image MIME types
            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/jpg" };

            // Check if the file's MIME type is in the allowed list
            return allowedMimeTypes.Contains(file.ContentType.ToLower());
        }
    }
}
