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

namespace Application.Features.SubCategoryFeat.DeleteCommands
{
    public record HardDeleteSubCategoryCommand(int subCategoryId) : IRequest<Result<SubCategoryDTO>>;

    public class HardDeleteSubCategoryCommandHandler : IRequestHandler<HardDeleteSubCategoryCommand, Result<SubCategoryDTO>>
    {
        private readonly ISubCategoryRepository _subCategoryRepository;
        public HardDeleteSubCategoryCommandHandler(ISubCategoryRepository subCategoryRepository)
        {
            _subCategoryRepository = subCategoryRepository;            
        }
        public async Task<Result<SubCategoryDTO>> Handle(HardDeleteSubCategoryCommand request, CancellationToken cancellationToken)
        {
            var subCategory = await _subCategoryRepository.FindByIdAsync(request.subCategoryId);
            if (subCategory == null)
                return Result<SubCategoryDTO>.Failure($"sub category with id : {request.subCategoryId} is not found");

            await _subCategoryRepository.HardDeleteSubCategoryAsync(request.subCategoryId, cancellationToken);
            return Result<SubCategoryDTO>.Success(subCategory.ToDTO(), $"sub category with id : {request.subCategoryId}is not found");
        }
    }

}
