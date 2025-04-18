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
    public record CreateCategoryCommand(
        string Name,
        string Slug,
        string Description
        
    ) : IRequest<Result<CategoryDTO>>;

    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<CategoryDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;

        public CreateCategoryCommandHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<CategoryDTO>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            // Create the new category
            var category = new Category
            {
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                
            };

            // Save the category to the database
            var createdCategory = await _categoryRepository.AddAsync(category, cancellationToken);

            if (createdCategory == null)
            {
                return Result<CategoryDTO>.Failure("Failed to create category.");
            }

            // Map to DTO and return success
            return Result<CategoryDTO>.Success(createdCategory.ToDTO(), "Category created successfully");
        }
    }
}
