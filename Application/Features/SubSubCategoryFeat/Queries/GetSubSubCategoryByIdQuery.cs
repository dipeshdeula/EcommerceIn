using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.SubSubCategoryFeat.Queries
{
    public record GetSubSubCategoryByIdQuery(int subSubCategoryId, int PageNumber, int PageSize) : IRequest<Result<SubSubCategoryDTO>>;

    public class GetSubSubCategoryByIdQueryHandler : IRequestHandler<GetSubSubCategoryByIdQuery, Result<SubSubCategoryDTO>>
    {
        private readonly ISubSubCategoryRepository _subSubCategoryRepository;
        private readonly ILogger<GetSubSubCategoryByIdQuery> _logger;

        public GetSubSubCategoryByIdQueryHandler(ISubSubCategoryRepository subSubCategoryRepository, ILogger<GetSubSubCategoryByIdQuery> logger)
        {
            _subSubCategoryRepository = subSubCategoryRepository;
            _logger = logger;

        }
        public async Task<Result<SubSubCategoryDTO>> Handle(GetSubSubCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            var subSubCategory = await _subSubCategoryRepository.FindByIdAsync(request.subSubCategoryId);
            if (subSubCategory == null)
            {
                return Result<SubSubCategoryDTO>.Failure($"SubSubCategoryId {request.subSubCategoryId} is not found");
            }

            return Result<SubSubCategoryDTO>.Success(subSubCategory.ToDTO(), $"SubSubCategory with id {request.subSubCategoryId} is fetch successfully");
        }
    }


}
