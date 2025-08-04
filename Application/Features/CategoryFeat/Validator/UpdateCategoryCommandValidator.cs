using Application.Features.CategoryFeat.UpdateCommands;
using Application.Interfaces.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.Validator
{
    public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
    {
        private readonly ICategoryRepository _categoryRepository;

        public UpdateCategoryCommandValidator(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;

            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("CategoryId must be greater than 0.")
                .MustAsync(async (id, cancellation) =>
                    await _categoryRepository.AnyAsync(c => c.Id == id))
                .WithMessage("Category does not exist.");

            When(x => !string.IsNullOrWhiteSpace(x.Name), () =>
            {
                RuleFor(x => x.Name!)
                    .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.")
                    /*.MustAsync(async (name, cancellation) =>
                        !await _categoryRepository.AnyAsync(c => c.ProviderName.ToLower() == name.ToLower()))
                    .WithMessage("Category name already exists.")*/
                    .MinimumLength(3).WithMessage("Name must be at least 3 characters long.");

            });

            When(x => !string.IsNullOrWhiteSpace(x.Slug), () =>
            {
                RuleFor(x => x.Slug!)
                    .MaximumLength(100).WithMessage("Slug cannot exceed 100 characters.")
                    .MinimumLength(3).WithMessage("Slug must be at least 3 characters long.");

            });

            When(x => !string.IsNullOrWhiteSpace(x.Description), () =>
            {
                RuleFor(x => x.Description!)
                    .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
                    .MinimumLength(150).WithMessage("Description must be at least 50 characters long.");

            });

            When(x => x.File != null, () =>
            {
                RuleFor(x => x.File!)
                    .Must(BeAnImage).WithMessage("Only image files (jpg, jpeg, png) are allowed.");
            });
        }

        private bool BeAnImage(IFormFile file)
        {
            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
            return file != null && allowedMimeTypes.Contains(file.ContentType.ToLower());
        }
    }
}
