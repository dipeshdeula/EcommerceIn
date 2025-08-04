using Application.Interfaces.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.UpdateCommands
{
    public class UpdateSubSubCategoryCommandValidator : AbstractValidator<UpdateSubSubCategoryCommand>
    {
        private readonly ISubSubCategoryRepository _subSubCategoryRepository;

        public UpdateSubSubCategoryCommandValidator(ISubSubCategoryRepository subSubCategoryRepository)
        {
            _subSubCategoryRepository = subSubCategoryRepository;

            RuleFor(x => x.SubSubCategoryId)
                .GreaterThan(0).WithMessage("SubSubCategoryId must be greater than 0.")
                .MustAsync(async (id, _) => await _subSubCategoryRepository.AnyAsync(s => s.Id == id))
                .WithMessage("Sub-subcategory does not exist.");

            When(x => !string.IsNullOrWhiteSpace(x.Name), () =>
            {
                RuleFor(x => x.Name!)
                    .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");
                    /*.MustAsync(async (command, name, _) =>
                        !await _subSubCategoryRepository.AnyAsync(s => s.ProviderName == name && s.Id != command.SubSubCategoryId))
                    .WithMessage("Another sub-subcategory with the same name already exists.");*/
            });

            When(x => !string.IsNullOrWhiteSpace(x.Slug), () =>
            {
                RuleFor(x => x.Slug!)
                    .MaximumLength(100).WithMessage("Slug cannot exceed 100 characters.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.Description), () =>
            {
                RuleFor(x => x.Description!)
                    .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
            });

            When(x => x.File != null, () =>
            {
                RuleFor(x => x.File!)
                    .Must(BeAnImage).WithMessage("Only image files (jpg, jpeg, png) are allowed.");
            });
        }

        private bool BeAnImage(IFormFile file)
        {
            var allowed = new[] { "image/jpeg", "image/png", "image/jpg" };
            return allowed.Contains(file.ContentType.ToLower());
        }
    }
}
