using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.SubSubCategoryFeat.Queries
{
    public record GetAllSubSubCategory(
       int PageNumber,
       int PageSize
        ) : IRequest<Result<IEnumerable<SubSubCategoryDTO>>>;

    public class GetAllSubSubCategoryHandler : IRequestHandler<GetAllSubSubCategory, Result<IEnumerable<SubSubCategoryDTO>>>
    {
        private readonly ISubSubCategoryRepository _subSubCategoryRepository;
        private readonly ILogger<GetAllSubSubCategory> _logger;
        public GetAllSubSubCategoryHandler(ISubSubCategoryRepository subSubCategoryRepository, ILogger<GetAllSubSubCategory> logger)
        {
            _subSubCategoryRepository = subSubCategoryRepository;
            _logger = logger;


        }
        public async Task<Result<IEnumerable<SubSubCategoryDTO>>> Handle(GetAllSubSubCategory request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching all sub sub categories with pagination.");

            // Fetch sub-categories with pagination
            var subSubCategories = await _subSubCategoryRepository.GetAllAsync(
                orderBy: query => query.OrderByDescending(SubSubCategory => SubSubCategory.Id),
                skip: (request.PageNumber - 1) * request.PageSize,
                take: request.PageSize,
                cancellationToken:cancellationToken);

            // Map Sub-Sub Categories to DTOs
            var subSubCategoryDTOs = subSubCategories.Select(ssc => ssc.ToDTO()).ToList();

            //Return the result wrapped in a Task
            return Result<IEnumerable<SubSubCategoryDTO>>.Success(subSubCategoryDTOs, "subSubCategory fetched successfully");
        }
    }


}
