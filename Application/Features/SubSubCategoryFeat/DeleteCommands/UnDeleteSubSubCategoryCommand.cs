using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.SubSubCategoryFeat.DeleteCommands
{
    public record UnDeleteSubSubCategoryCommand (int subSubCategoryId) : IRequest<Result<SubSubCategoryDTO>>;

    public class UnDeleteSubSubCategoryCommandHandler : IRequestHandler<UnDeleteSubSubCategoryCommand, Result<SubSubCategoryDTO>>
    {
        private readonly ISubSubCategoryRepository _subSubCategoryRepository;
        public UnDeleteSubSubCategoryCommandHandler(ISubSubCategoryRepository subSubCategoryRepository)
        {
            _subSubCategoryRepository = subSubCategoryRepository;
            
        }
        public async Task<Result<SubSubCategoryDTO>> Handle(UnDeleteSubSubCategoryCommand request, CancellationToken cancellationToken)
        {
            var subSubCategory = await _subSubCategoryRepository.FindByIdAsync(request.subSubCategoryId);
            if (subSubCategory == null)
                return Result<SubSubCategoryDTO>.Failure($"subSubCategory with Id : {request.subSubCategoryId} is not found");

            await _subSubCategoryRepository.UndeleteSubSubCategoryAsync(request.subSubCategoryId, cancellationToken);

            return Result<SubSubCategoryDTO>.Success(subSubCategory.ToDTO(), $"subSubCategory with Id : {request.subSubCategoryId} is undeleted successully");
        }
    }
}
