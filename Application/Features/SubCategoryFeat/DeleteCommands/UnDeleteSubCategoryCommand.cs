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
    public record UnDeleteSubCategoryCommand (int subCategoryId) : IRequest<Result<SubCategoryDTO>>;

    public class UnDeleteSubCategoryCommandHandler : IRequestHandler<UnDeleteSubCategoryCommand, Result<SubCategoryDTO>>
    {
        private readonly ISubCategoryRepository _subCategoryRepository;
        public UnDeleteSubCategoryCommandHandler(ISubCategoryRepository subCategoryRepository)
        {
            _subCategoryRepository = subCategoryRepository;
            
        }
        public async Task<Result<SubCategoryDTO>> Handle(UnDeleteSubCategoryCommand request, CancellationToken cancellationToken)
        {
            var subCategory = await _subCategoryRepository.FindByIdAsync(request.subCategoryId);
            if (subCategory == null)
            {
                return Result<SubCategoryDTO>.Failure($"sub category with id {request.subCategoryId} is not found");

            }
            await _subCategoryRepository.UndeleteSubCategoryAsync(request.subCategoryId);

            return Result<SubCategoryDTO>.Success(subCategory.ToDTO(), $"sub category with id : {request.subCategoryId} is undeleted successfully");
        }
    }

}
