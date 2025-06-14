using Application.Common;
using Application.Dto.CategoryDTOs;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.DeleteCommands
{
    public record HardDeleteCategoryCommand(int CategoryId) : IRequest<Result<CategoryDTO>>;

    public class HardDeleteCategoryCommandHandler(ICategoryRepository _categoryRepository) : IRequestHandler<HardDeleteCategoryCommand, Result<CategoryDTO>>
        {

        public async Task<Result<CategoryDTO>> Handle(HardDeleteCategoryCommand request, CancellationToken cancellationToken)
        {
             await _categoryRepository.HardDeleteCategoryAsync(request.CategoryId, cancellationToken);          
            return Result<CategoryDTO>.Success(null, "Category are hard deleted successfully");


        }
    }


}
