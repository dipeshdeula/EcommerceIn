using Application.Common;
using Application.Dto;
using Application.Enums;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.UpdateCommands
{
    public record UpdateSubCategoryCommand(
        int SubCategoryId,
        string Name,
        string Slug,
        string Description,
        IFormFile File

        ) : IRequest<Result<SubCategoryDTO>>;

    public class UpdateSubCategoryCommandHandler : IRequestHandler<UpdateSubCategoryCommand, Result<SubCategoryDTO>>
    {
        private readonly ISubCategoryRepository _subCategoryRepository;
        private readonly IFileServices _fileService;
        private readonly ILogger<UpdateSubCategoryCommand> _logger;
        public UpdateSubCategoryCommandHandler(ISubCategoryRepository subCategoryRepository, IFileServices fileService, ILogger<UpdateSubCategoryCommand> logger)
        {
            _subCategoryRepository = subCategoryRepository;
            _fileService = fileService;
            _logger = logger;
            
        }
        public async Task<Result<SubCategoryDTO>> Handle(UpdateSubCategoryCommand request, CancellationToken cancellationToken)
        {
            var subCategory = await _subCategoryRepository.FindByIdAsync(request.SubCategoryId);
            if (subCategory == null)
            {
                return Result<SubCategoryDTO>.Failure("SubCategory not found");
            }

            // Update fields only if they are provided
            subCategory.Name = request.Name ?? subCategory.Name;
            subCategory.Slug = request.Slug ?? subCategory.Slug;
            subCategory.Description = request.Description ?? subCategory.Description;

            // Handle image update
            if (request.File != null)
            {
                try
                {
                    subCategory.ImageUrl = await _fileService.UpdateFileAsync(subCategory.ImageUrl, request.File, FileType.SubCategoryImages);
                }
                catch (Exception ex)
                { 
                    _logger.LogError($"Image update failed:{ ex.Message}");
                    return Result<SubCategoryDTO>.Failure("Image updated failed");
                }
            }

            // Save changes to the database
            await _subCategoryRepository.UpdateAsync(subCategory, cancellationToken);

            // Map updated subcategory to DTO
            var subCategoryDTO = new SubCategoryDTO
            {
                Id = subCategory.Id,
                Name = subCategory.Name,
                Slug = subCategory.Slug,
                Description = subCategory.Description,
                ImageUrl = subCategory.ImageUrl
            };

            return Result<SubCategoryDTO>.Success(subCategoryDTO, "SubCategory updated successfully");
        }
    }

}
