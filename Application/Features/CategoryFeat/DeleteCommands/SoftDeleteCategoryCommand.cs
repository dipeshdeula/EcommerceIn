using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.DeleteCommands
{
    public record SoftDeleteCategoryCommand(int CategoryId) : IRequest<Result<CategoryDTO>>;


    public class SoftDeleteCategoryCommandHandler : IRequestHandler<SoftDeleteCategoryCommand, Result<CategoryDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;
        public SoftDeleteCategoryCommandHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
            
            
        }
        public async Task<Result<CategoryDTO>> Handle(SoftDeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _categoryRepository.FindByIdAsync(request.CategoryId);
            if (category == null)
                return Result<CategoryDTO>.Failure("Category not found");
            await _categoryRepository.SoftDeleteCategoryAsync(request.CategoryId, cancellationToken);
            return Result<CategoryDTO>.Success(category.ToDTO(), $"Category with Id {category.Id} soft deleted successful");
        }
    }
}
