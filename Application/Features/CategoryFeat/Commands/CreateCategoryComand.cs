using Application.Common;
using Application.Dto.CategoryDTOs;
using Application.Enums;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.Commands
{
    public record CreateCategoryCommand(
         string Name,
         string Slug,
         string Description,
         IFormFile File
       
        
    ) : IRequest<Result<CategoryDTO>>;

    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<CategoryDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IFileServices _fileService;

        public CreateCategoryCommandHandler(ICategoryRepository categoryRepository,IFileServices fileService)
        {
            _categoryRepository = categoryRepository;
            _fileService = fileService;
        }

        public async Task<Result<CategoryDTO>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            string fileUrl = null;
            if (request.File != null && request.File.Length>0)
            {
                try
                {
                    fileUrl = await _fileService.SaveFileAsync(request.File, FileType.CategoryImages);

                }
                catch (Exception ex)
                {
                    return Result<CategoryDTO>.Failure($"Image upload failed: {ex.Message}");

                }
            }
            // Create the new category
            var category = new Category
            {
                
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                ImageUrl = fileUrl

            };
            Console.WriteLine("Id:", category.Id);
            // Save the category to the database
            var createdCategory = await _categoryRepository.AddAsync(category, cancellationToken);
            await _categoryRepository.SaveChangesAsync(cancellationToken);

            if (createdCategory == null)
            {
                return Result<CategoryDTO>.Failure("Failed to create category.");
            }

            // Map to DTO and return success
            return Result<CategoryDTO>.Success(createdCategory.ToDTO(), "Category created successfully");
        }
    }
}
