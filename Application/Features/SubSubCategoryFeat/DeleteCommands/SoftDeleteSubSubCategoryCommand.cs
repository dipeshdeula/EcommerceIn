using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.SubSubCategoryFeat.DeleteCommands
{
    public record SoftDeleteSubSubCategoryCommand ( int subSubCategoryId) : IRequest<Result<SubSubCategoryDTO>>;

    public class SoftDeleteSubSubCategoryCommandHandler : IRequestHandler<SoftDeleteSubSubCategoryCommand, Result<SubSubCategoryDTO>>
    {
        private readonly ISubSubCategoryRepository _subSubCategoryRepository;
        public SoftDeleteSubSubCategoryCommandHandler(ISubSubCategoryRepository subSubCategoryRepository)
        {
            _subSubCategoryRepository = subSubCategoryRepository;
            
        }

        public async Task<Result<SubSubCategoryDTO>> Handle(SoftDeleteSubSubCategoryCommand request, CancellationToken cancellationToken)
        {
            var subSubCategory = await _subSubCategoryRepository.FindByIdAsync(request.subSubCategoryId);
            if (subSubCategory == null)
            {
                return Result<SubSubCategoryDTO>.Failure($"sub sub category with id : {request.subSubCategoryId} is not found");
            }

            await _subSubCategoryRepository.SoftDeleteSubSubCategoryAsync(request.subSubCategoryId, cancellationToken);

            return Result<SubSubCategoryDTO>.Success(subSubCategory.ToDTO(), $"sub subc ategory with Id {subSubCategory.Id} soft deleted successful");

        }
    }
}
