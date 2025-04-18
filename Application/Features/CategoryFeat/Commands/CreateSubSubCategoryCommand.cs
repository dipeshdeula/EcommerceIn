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
    public record CreateSubSubCategoryCommand(
        int SubCategoryId,
        string Name,
        string Slug,
        string Description
    ) : IRequest<Result<SubSubCategoryDTO>>;

    public class CreateSubSubCategoryCommandHandler : IRequestHandler<CreateSubSubCategoryCommand, Result<SubSubCategoryDTO>>
    {
        private readonly ISubCategoryRepository _subCategoryRepository;

        public CreateSubSubCategoryCommandHandler(ISubCategoryRepository subCategoryRepository)
        {
            _subCategoryRepository = subCategoryRepository;
        }

        public async Task<Result<SubSubCategoryDTO>> Handle(CreateSubSubCategoryCommand request, CancellationToken cancellationToken)
        {
            // Validate SubCategoryId
            var subCategory = await _subCategoryRepository.FindByIdAsync(request.SubCategoryId);
            if (subCategory == null)
            {
                return Result<SubSubCategoryDTO>.Failure("Sub-category not found.");
            }

            // Create the new SubSubCategory
            var subSubCategory = new SubSubCategory
            {
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                SubCategoryId = request.SubCategoryId,
                SubCategory = subCategory
            };

            // Add the SubSubCategory to the parent's SubSubCategories collection
            subCategory.SubSubCategories.Add(subSubCategory);

            // Save changes to the database
            await _subCategoryRepository.UpdateAsync(subCategory, cancellationToken);

            // Map to DTO and return success
            return Result<SubSubCategoryDTO>.Success(subSubCategory.ToDTO(), "SubSubCategory created successfully");
        }
    }
}
