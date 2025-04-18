using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.CategoryFeat.Queries
{
    public record GetAllSubCategoryQuery(
        int PageNumber,
        int PageSize
        ) : IRequest<Result<IEnumerable<SubCategoryDTO>>>;

    public class GetAllSubCategoryHandler : IRequestHandler<GetAllSubCategoryQuery, Result<IEnumerable<SubCategoryDTO>>>
    {
        private readonly ISubCategoryRepository _subCategoryRepository;
        private readonly ILogger<GetAllSubCategoryQuery> _logger;

        public GetAllSubCategoryHandler(ISubCategoryRepository subCategoryRepository,ILogger<GetAllSubCategoryQuery> logger)
        {
            _subCategoryRepository = subCategoryRepository;
            _logger = logger;
            
        }

        public async Task<Result<IEnumerable<SubCategoryDTO>>> Handle(GetAllSubCategoryQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching all sub categories with pagination");

            // Fetch sub-categories with pagination
            var subCategories = await _subCategoryRepository.GetAllAsync(
                orderBy: query => query.OrderByDescending(subCategory => subCategory.Id),
                skip: (request.PageNumber - 1) * request.PageSize,
                take: request.PageSize);

            // Map subCategories to DTOs
            var subCategoryDTOs = subCategories.Select(cd => cd.ToDTO()).ToList();

            //Return the result wrapped in a Task
            return Result<IEnumerable<SubCategoryDTO>>.Success(subCategoryDTOs, "Sub-Categories fetched successfully");

                
        }
    }
    
}
