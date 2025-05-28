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
    public record HardDeleteSubSubCategoryCommand(int subSubCategoryId) : IRequest<Result<SubSubCategoryDTO>>;

    public class HardDeleteSubSubCategoryCommandHandler : IRequestHandler<HardDeleteSubSubCategoryCommand, Result<SubSubCategoryDTO>>
    {
        private readonly ISubSubCategoryRepository _subSubCategoryRepository;
        public HardDeleteSubSubCategoryCommandHandler(ISubSubCategoryRepository subSubCategoryRepository)
        {
            _subSubCategoryRepository = subSubCategoryRepository;
            
        }
        public async Task<Result<SubSubCategoryDTO>> Handle(HardDeleteSubSubCategoryCommand request, CancellationToken cancellationToken)
        {
            var subSubCategory = await _subSubCategoryRepository.FindByIdAsync(request.subSubCategoryId);
            if (subSubCategory == null)
                return Result<SubSubCategoryDTO>.Failure($"subSubCategory with id : {request.subSubCategoryId} is not found");

            await _subSubCategoryRepository.HardDeleteSubSubCategoryAsync(request.subSubCategoryId, cancellationToken);

            return Result<SubSubCategoryDTO>.Success(subSubCategory.ToDTO(), $"SubSubCategory with id : {request.subSubCategoryId} is deleted successfully");

        }
    }
}
