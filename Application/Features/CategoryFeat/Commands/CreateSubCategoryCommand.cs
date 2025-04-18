using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.Commands
{
    public record CreateSubCategoryCommand(
        string Name,
        string Slug,
        string Description,
        int ParentCategoryId // Parent category is required for subcategories
    ) : IRequest<Result<SubCategoryDTO>>;

    public class CreateSubCategoryCommandHandler : IRequestHandler<CreateSubCategoryCommand, Result<SubCategoryDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;

        public CreateSubCategoryCommandHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<SubCategoryDTO>> Handle(CreateSubCategoryCommand request, CancellationToken cancellationToken)
        {
            // Validate ParentCategoryId
            var parentCategory = await _categoryRepository.FindByIdAsync(request.ParentCategoryId);
            if (parentCategory == null)
            {
                return Result<SubCategoryDTO>.Failure("Parent category not found.");
            }

            // Create the new subcategory
            var subCategory = new SubCategory
            {
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                CategoryId = request.ParentCategoryId,
                Category = parentCategory
            };

            // Add the subcategory to the parent's SubCategories collection
            parentCategory.SubCategories.Add(subCategory);

            // Save changes to the database
            await _categoryRepository.UpdateAsync(parentCategory, cancellationToken);

            // Map to DTO and return success
            return Result<SubCategoryDTO>.Success(subCategory.ToDTO(), "Subcategory created successfully");
        }
    }
}
