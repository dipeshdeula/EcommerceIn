using Application.Common;
using Application.Dto.CategoryDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.CategoryFeat.Queries
{
    public record GetCategoryByIdQuery
        (
        int CategoryId,
        int PageNumber,
        int PageSize
        ) : IRequest<Result<CategoryDTO>>;


    public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Result<CategoryDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<GetCategoryByIdQuery> _logger;
        public GetCategoryByIdQueryHandler(ICategoryRepository categoryRepository,ILogger<GetCategoryByIdQuery> logger)
        {
            _categoryRepository = categoryRepository;
            _logger = logger;
            
        }


        public async Task<Result<CategoryDTO>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            var category = await _categoryRepository.FindByIdAsync(request.CategoryId);
            if (category == null)
            {
                return Result<CategoryDTO>.Failure("Category Id not found");
            }           

            return Result<CategoryDTO>.Success(category.ToDTO(), $"category ID {category.Id} is fetched successfully");
        }
    }
}
