using Application.Common;
using Application.Dto;
using Application.Enums;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.SubSubCategoryFeat.Commands
{
    public record CreateSubSubCategoryCommand(
        int SubCategoryId,
        string Name,
        string Slug,
        string Description,
        IFormFile File
    ) : IRequest<Result<SubSubCategoryDTO>>;

    public class CreateSubSubCategoryCommandHandler : IRequestHandler<CreateSubSubCategoryCommand, Result<SubSubCategoryDTO>>
    {
        private readonly ISubCategoryRepository _subCategoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileServices _fileService;

        public CreateSubSubCategoryCommandHandler(
            ISubCategoryRepository subCategoryRepository, 
            IUnitOfWork unitOfWork,
            IFileServices fileService)
        {
            _subCategoryRepository = subCategoryRepository;
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }

        public async Task<Result<SubSubCategoryDTO>> Handle(CreateSubSubCategoryCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate SubCategoryId
                var subCategory = await _subCategoryRepository.FindByIdAsync(request.SubCategoryId);
                if (subCategory == null)
                {
                    return Result<SubSubCategoryDTO>.Failure("Sub-category not found.");
                }

                string fileUrl = null!;

                if (request.File != null && request.File.Length > 0)
                {
                    try
                    {
                        fileUrl = await _fileService.SaveFileAsync(request.File, FileType.SubSubCategoryImages);

                    }
                    catch (Exception ex)
                    {
                        return Result<SubSubCategoryDTO>.Failure($"Image upload failed: {ex.Message}");

                    }
                }

                // Create the new SubSubCategory
                var subSubCategory = new SubSubCategory
                {
                    Name = request.Name,
                    Slug = request.Slug,
                    Description = request.Description,
                    ImageUrl = fileUrl!,
                    SubCategoryId = request.SubCategoryId,
                    SubCategory = subCategory,
                    CategoryId = subCategory.CategoryId,
                    Category = subCategory.Category

                };

                // Add the SubSubCategory to the parent's SubSubCategories collection
                await _unitOfWork.SubSubCategories.AddAsync(subSubCategory, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Map to DTO and return success
                return Result<SubSubCategoryDTO>.Success(subSubCategory.ToDTO(), "SubSubCategory created successfully");
            }
            catch (Exception ex)
            {
                return Result<SubSubCategoryDTO>.Failure("Fail to create subSubCategory", ex.Message);
            }
           
        }
    }
}
