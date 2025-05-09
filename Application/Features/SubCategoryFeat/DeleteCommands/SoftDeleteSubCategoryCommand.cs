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
    public record SoftDeleteSubCategoryCommand (int subCategorId) : IRequest<Result<SubCategoryDTO>>;

    public class SoftDeleteSubCategoryCommandHandler : IRequestHandler<SoftDeleteSubCategoryCommand, Result<SubCategoryDTO>>
    {
        private readonly ISubCategoryRepository _subCategoryRepository;
        public SoftDeleteSubCategoryCommandHandler(ISubCategoryRepository subCategoryRepository)
        {
            _subCategoryRepository = subCategoryRepository;            
        }
        public async Task<Result<SubCategoryDTO>> Handle(SoftDeleteSubCategoryCommand request, CancellationToken cancellationToken)
        {
            var subCategory = await _subCategoryRepository.FindByIdAsync(request.subCategorId);
            if (subCategory == null)
            {
                return Result<SubCategoryDTO>.Failure("sub category not found");
                
            }

            await _subCategoryRepository.SoftDeleteSubCategoryAsync(request.subCategorId, cancellationToken);

            return Result<SubCategoryDTO>.Success(subCategory.ToDTO(), $"subCategory with id {request.subCategorId} soft deleted successfully");
        }
    }
}
