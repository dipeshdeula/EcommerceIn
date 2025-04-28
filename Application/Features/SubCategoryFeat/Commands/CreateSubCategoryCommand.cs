using Application.Common;
using Application.Dto;
using Application.Enums;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.SubCategoryFeat.Commands
{
    public record CreateSubCategoryCommand(
        int ParentCategoryId,
        string Name,
        string Slug,
        string Description,
        IFormFile File

    ) : IRequest<Result<SubCategoryDTO>>;

    public class CreateSubCategoryCommandHandler : IRequestHandler<CreateSubCategoryCommand, Result<SubCategoryDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IFileServices _fileService;

        public CreateSubCategoryCommandHandler(ICategoryRepository categoryRepository, IFileServices fileService)
        {
            _categoryRepository = categoryRepository;
            _fileService = fileService;
        }

        public async Task<Result<SubCategoryDTO>> Handle(CreateSubCategoryCommand request, CancellationToken cancellationToken)
        {

            var parentCategory = await _categoryRepository.FindByIdAsync(request.ParentCategoryId);
            if (parentCategory == null)
            {
                return Result<SubCategoryDTO>.Failure("Parent category not found.");
            }
            string fileUrl = null;
            if (request.File != null && request.File.Length > 0)
            {
                try
                {
                    fileUrl = await _fileService.SaveFileAsync(request.File, FileType.SubCategoryImages);

                }
                catch (Exception ex)
                {
                    return Result<SubCategoryDTO>.Failure($"Image upload failed: {ex.Message}");

                }
            }
            // Validate ParentCategoryId

            // Create the new subcategory
            var subCategory = new SubCategory
            {
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                CategoryId = request.ParentCategoryId,
                Category = parentCategory,
                ImageUrl = fileUrl
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
