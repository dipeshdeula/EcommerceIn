using Application.Common;
using Application.Dto;
using Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.DeleteCommands
{
    public record UnDeleteCategoryCommand(int CategoryId) : IRequest<Result<CategoryDTO>>;

    public class UnDeleteCategoryCommandHandler(ICategoryRepository _categoryRepository) : IRequestHandler<UnDeleteCategoryCommand, Result<CategoryDTO>>
    {
        public async Task<Result<CategoryDTO>> Handle(UnDeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var success = await _categoryRepository.UndeleteCategoryAsync(request.CategoryId, cancellationToken);
            if (!success)
            {
                return Result<CategoryDTO>.Failure("Category not found");
            }
            return Result<CategoryDTO>.Success(null, "Category are undeleted successfully");
        }
    }



}
