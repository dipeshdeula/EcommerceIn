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

namespace Application.Features.CategoryFeat.DeleteCategoryCommands
{
    public record SoftDeleteCategoryCommand(int CategoryId) : IRequest<Result<CategoryDTO>>;

    public class SoftDeleteCategoryCommandHandler : IRequestHandler<SoftDeleteCategoryCommand, Result<CategoryDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IFileServices _fileServices;
        public SoftDeleteCategoryCommandHandler(ICategoryRepository categoryRepository,IFileServices fileServices)
        {
            _categoryRepository = categoryRepository;
            _fileServices = fileServices;
            
        }
        public async Task<Result<CategoryDTO>> Handle(SoftDeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _categoryRepository.FindByIdAsync(request.CategoryId);
            if (category == null)
            {
                return Result<CategoryDTO>.Failure("Category Id not found");
            }

            await _categoryRepository.SoftDeleteAsync(category, cancellationToken);
            return Result<CategoryDTO>.Success(category.ToDTO(), "Category Soft deleted successfully");
        }
    }

}
