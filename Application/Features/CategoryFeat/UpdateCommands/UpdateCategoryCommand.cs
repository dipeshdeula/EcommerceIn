using Application.Common;
using Application.Dto;
using Application.Enums;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Application.Features.CategoryFeat.UpdateCommands
{
    public record UpdateCategoryCommand(
        int CategoryId,
        string? Name,
        string? Slug,
        string? Description,
        IFormFile? File
        ) : IRequest<Result<CategoryDTO>>;

    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result<CategoryDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IFileServices _fileService;
        private readonly ILogger<UpdateCategoryCommand> _logger;

        public UpdateCategoryCommandHandler(ICategoryRepository categoryRepository, IFileServices fileService, ILogger<UpdateCategoryCommand> logger)
        {
            _categoryRepository = categoryRepository;
            _fileService = fileService;
            _logger = logger;
        }


        public async Task<Result<CategoryDTO>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            // Fetch the category from the database
            var category = await _categoryRepository.FindByIdAsync(request.CategoryId);
            if (category == null)
            {
                return Result<CategoryDTO>.Failure("Category not found.");
            }

            // Update fields only if they are provided
            category.Name = request.Name ?? category.Name;
            category.Slug = request.Slug ?? category.Slug;
            category.Description = request.Description ?? category.Description;

            // Handle image update
            if (request.File != null)
            {
                try
                {
                    category.ImageUrl = await _fileService.UpdateFileAsync(category.ImageUrl, request.File, FileType.CategoryImages);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Image update failed: {ex.Message}");
                    return Result<CategoryDTO>.Failure("Image update failed.");
                }
            }

            // Save changes to the database
            await _categoryRepository.UpdateAsync(category, cancellationToken);

            // Map updated category to DTO
            var categoryDTO = new CategoryDTO
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                ImageUrl = category.ImageUrl
            };

            return Result<CategoryDTO>.Success(categoryDTO, "Category updated successfully.");
        }
    }


}




