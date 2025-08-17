using Application.Common;
using Application.Dto;
using Application.Dto.CategoryDTOs;
using Application.Features.CategoryFeat.Queries;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.SubSubCategoryFeat.Queries
{
    public record GetAllSubSubCategoryBySubCategoryId(
        int SubCategoryId, int PageNumber, int PageSize) : IRequest<Result<SubSubCategoryWithSubCategoryDTO>>;

    public class GetAllSubSubCategoryBySubCategoryIdHandler : IRequestHandler<GetAllSubSubCategoryBySubCategoryId, Result<SubSubCategoryWithSubCategoryDTO>>
    {
        private readonly ISubCategoryRepository _subcategoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllSubSubCategoryByCategoryId> _logger;

        public GetAllSubSubCategoryBySubCategoryIdHandler(
             ISubCategoryRepository subCategoryRepository,
            IUnitOfWork unitOfWork,
            ILogger<GetAllSubSubCategoryByCategoryId> logger
            )
        {
            _subcategoryRepository = subCategoryRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task<Result<SubSubCategoryWithSubCategoryDTO>> Handle(GetAllSubSubCategoryBySubCategoryId request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching sub-sub categories for SubCategory {SubCategoryId} - Page {PageNumber}, Size {PageSize}", request.SubCategoryId, request.PageNumber, request.PageSize);

                // Check whether category exists
                var subCategory = await _subcategoryRepository.FindByIdAsync(request.SubCategoryId);
                if (subCategory == null)
                {
                    return Result<SubSubCategoryWithSubCategoryDTO>.Failure("SubCategoryId is not found");
                }
                // Fetch subcategories with pagination and deletion filter
                var subSubCategoriesQuery = _unitOfWork.SubSubCategories.GetQueryable()
                    .Where(sc => sc.SubCategoryId == request.SubCategoryId && !sc.IsDeleted);

                var totalSubSubCategories = await _unitOfWork.SubSubCategories.CountAsync(
                    predicate: sc => sc.SubCategoryId == request.SubCategoryId && !sc.IsDeleted,
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
                        SubCategoryId = sc.SubCategoryId
                    })
                    .ToList();

                var resultDto = new SubSubCategoryWithSubCategoryDTO
                {
                    Id = subCategory.Id,
                    Name = subCategory.Name,
                    Slug = subCategory.Slug,
                    Description = subCategory.Description,
                    SubSubCategories = subSubCategories,
                    TotalSubSubCategories = totalSubSubCategories
                };

                return Result<SubSubCategoryWithSubCategoryDTO>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subcategories for category {CategoryId}", request.SubCategoryId);
                return Result<SubSubCategoryWithSubCategoryDTO>.Failure($"Failed to fetch subcategories: {ex.Message}");
            }
        }
    }


}
