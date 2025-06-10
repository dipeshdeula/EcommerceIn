using Application.Common;
using Application.Dto.CategoryDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.Queries
{
    public record GetAllCategoryQuery(
        int PageNumber,
        int PageSize
    ) : IRequest<Result<IEnumerable<CategoryDTO>>>;

    public class GetAllCategoryQueryHandler : IRequestHandler<GetAllCategoryQuery, Result<IEnumerable<CategoryDTO>>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<GetAllCategoryQuery> _logger;

        public GetAllCategoryQueryHandler(ICategoryRepository categoryRepository, ILogger<GetAllCategoryQuery> logger)
        {
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<CategoryDTO>>> Handle(GetAllCategoryQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching all categories with pagination");

            // Fetch categories with pagination
            var categories = await _categoryRepository.GetAllAsync(
                orderBy: query => query.OrderByDescending(category => category.Id),
                skip: (request.PageNumber - 1) * request.PageSize,
                take: request.PageSize,
                cancellationToken:cancellationToken
            );

            // Map categories to DTOs
            var categoryDTOs = categories.Select(cd => cd.ToDTO()).ToList();

            // Return the result wrapped in a Task
            return Result<IEnumerable<CategoryDTO>>.Success(categoryDTOs, "Categories fetched successfully");
        }
    }
}
