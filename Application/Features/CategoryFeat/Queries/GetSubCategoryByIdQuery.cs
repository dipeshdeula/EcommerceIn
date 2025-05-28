using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.CategoryFeat.Queries
{
    public record GetSubCategoryByIdQuery (
        int SubCategoryId,
        int PageNumber,
        int PageSize
        ) : IRequest<Result<SubCategoryDTO>>;

    public class GetSubCategoryByIdQueryHandler : IRequestHandler<GetSubCategoryByIdQuery, Result<SubCategoryDTO>>
    {
        private readonly ISubCategoryRepository _subCategoryRepository;
        private readonly ILogger<GetSubCategoryByIdQuery> _logger;
        public GetSubCategoryByIdQueryHandler(ISubCategoryRepository subCategoryRepository,ILogger<GetSubCategoryByIdQuery> logger)
        {
            _subCategoryRepository = subCategoryRepository;
            _logger = logger;
            
        }

        public async Task<Result<SubCategoryDTO>> Handle(GetSubCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            var subCategory = await _subCategoryRepository.FindByIdAsync(request.SubCategoryId);

            if (subCategory == null)
            {
                return Result<SubCategoryDTO>.Failure($"SubCategory Id {request.SubCategoryId} not found");
            }

            return Result<SubCategoryDTO>.Success(subCategory.ToDTO(), $"SubCategory Id {request.SubCategoryId} is fetched successfully");
        }
    }

}
