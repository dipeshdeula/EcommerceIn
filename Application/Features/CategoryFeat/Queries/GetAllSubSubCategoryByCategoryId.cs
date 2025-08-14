using Application.Common;
using Application.Dto;
using Application.Dto.CategoryDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Sprache;

namespace Application.Features.CategoryFeat.Queries
{
    public record GetAllSubSubCategoryByCategoryId(
        int CategoryId,
        int PageNumber,
        int PageSize
        ) : IRequest<Result<CategoryWithSubSubCategoryDTO>>;

    public class GetAllSubSubCategoryByCategoryIdHandler : IRequestHandler<GetAllSubSubCategoryByCategoryId, Result<CategoryWithSubSubCategoryDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllSubSubCategoryByCategoryId> _logger;

        public GetAllSubSubCategoryByCategoryIdHandler(
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork,
            ILogger<GetAllSubSubCategoryByCategoryId> logger

            )
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            
        }

        public async Task<Result<CategoryWithSubSubCategoryDTO>> Handle(GetAllSubSubCategoryByCategoryId request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching sub-sub categories for Category {CategoryId} - Page {PageNumber}, Size {PageSize}", request.CategoryId, request.PageNumber, request.PageSize);

                // Check whether category exists
                var category = await _categoryRepository.FindByIdAsync(request.CategoryId);
                if (category == null)
                {
                    return Result<CategoryWithSubSubCategoryDTO>.Failure("CategoryId is not found");
                }
                // Fetch subcategories with pagination and deletion filter
                var subSubCategoriesQuery = _unitOfWork.SubSubCategories.GetQueryable()
                    .Where(sc => sc.CategoryId == request.CategoryId && !sc.IsDeleted);

                var totalSubSubCategories = await _unitOfWork.SubSubCategories.CountAsync(
                    predicate: sc => sc.CategoryId == request.CategoryId && !sc.IsDeleted,
                    cancellationToken: cancellationToken);

                var subSubCategories = subSubCategoriesQuery
                    .OrderBy(sc => sc.Id)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(sc => new SubSubCategoryDTO
                    {
                        Id = sc.Id,
                        Name = sc.Name,
                        Slug = sc.Slug,
                        Description = sc.Description,
                        CategoryId = sc.CategoryId
                    })
                    .ToList();

                var resultDto = new CategoryWithSubSubCategoryDTO
                {
                    Id = category.Id,
                    Name = category.Name,
                    Slug = category.Slug,
                    Description = category.Description,
                    SubSubCategories = subSubCategories,
                    TotalSubSubCategories = totalSubSubCategories
                };

                return Result<CategoryWithSubSubCategoryDTO>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subcategories for category {CategoryId}", request.CategoryId);
                return Result<CategoryWithSubSubCategoryDTO>.Failure($"Failed to fetch subcategories: {ex.Message}");
            }
        }
    }
}
