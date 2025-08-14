using Application.Common;
using Application.Dto;
using Application.Dto.CategoryDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.CategoryFeat.Queries
{
    public record GetAllSubCategoryByCategoryId(
        int CategoryId,
        int PageNumber,
        int PageSize
        
    ) : IRequest<Result<CategoryWithSubCategoryDTO>>;

    public class GetAllSubCategoryByCategoryIdHandler : IRequestHandler<GetAllSubCategoryByCategoryId, Result<CategoryWithSubCategoryDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllSubCategoryByCategoryId> _logger;

        public GetAllSubCategoryByCategoryIdHandler(
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork,
            ILogger<GetAllSubCategoryByCategoryId> logger
        )
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<CategoryWithSubCategoryDTO>> Handle(GetAllSubCategoryByCategoryId request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching subcategories for Category {CategoryId} - Page {PageNumber}, Size {PageSize}", request.CategoryId, request.PageNumber, request.PageSize);

                // Check whether category exists
                var category = await _categoryRepository.FindByIdAsync(request.CategoryId);
                if (category == null)
                {
                    return Result<CategoryWithSubCategoryDTO>.Failure("CategoryId is not found");
                }

                // Fetch subcategories with pagination and deletion filter
                var subCategoriesQuery = _unitOfWork.SubCategories.GetQueryable()
                    .Where(sc => sc.CategoryId == request.CategoryId &&  !sc.IsDeleted);

                var totalSubCategories = await _unitOfWork.SubCategories.CountAsync(
                    predicate: sc => sc.CategoryId == request.CategoryId &&  !sc.IsDeleted,
                    cancellationToken: cancellationToken);

                var subCategories = subCategoriesQuery
                    .OrderBy(sc => sc.Id)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(sc => new SubCategoryDTO
                    {
                        Id = sc.Id,
                        Name = sc.Name,
                        Slug = sc.Slug,
                        Description = sc.Description,
                        CategoryId = sc.CategoryId
                    })
                    .ToList();

                var resultDto = new CategoryWithSubCategoryDTO
                {
                    Id = category.Id,
                    Name = category.Name,
                    Slug = category.Slug,
                    Description = category.Description,
                    SubCategories = subCategories,
                    TotalSubCategories = totalSubCategories
                };

                return Result<CategoryWithSubCategoryDTO>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subcategories for category {CategoryId}", request.CategoryId);
                return Result<CategoryWithSubCategoryDTO>.Failure($"Failed to fetch subcategories: {ex.Message}");
            }
        }
    }
}