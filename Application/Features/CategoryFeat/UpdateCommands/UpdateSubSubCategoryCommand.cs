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
    public record UpdateSubSubCategoryCommand(
        int SubSubCategoryId,
        string? Name,
        string? Slug,
        string? Description,
        IFormFile? File
        ) : IRequest<Result<SubSubCategoryDTO>>;

    public class UpdateSubSubCategoryCommandHandler : IRequestHandler<UpdateSubSubCategoryCommand, Result<SubSubCategoryDTO>>
    {
        private readonly ISubSubCategoryRepository _subSubCategoryRepository;
        private readonly IFileServices _fileService;
        private readonly ILogger<UpdateSubSubCategoryCommand> _logger;

        public UpdateSubSubCategoryCommandHandler(ISubSubCategoryRepository subSubCategoryRepository, IFileServices fileService, ILogger<UpdateSubSubCategoryCommand> logger)
        {
            _subSubCategoryRepository = subSubCategoryRepository;
            _fileService = fileService;
            _logger = logger;
            
        }

        public async Task<Result<SubSubCategoryDTO>> Handle(UpdateSubSubCategoryCommand request, CancellationToken cancellationToken)
        {
            var subSubCategory = await _subSubCategoryRepository.FindByIdAsync(request.SubSubCategoryId);
            if (subSubCategory == null)
            {
                return Result<SubSubCategoryDTO>.Failure("SubSubCategory Id not found");
            
            }

            // Update feilds only if they are provided
            subSubCategory.Name = request.Name ?? subSubCategory.Name;
            subSubCategory.Slug = request.Slug ?? subSubCategory.Slug;
            subSubCategory.Description = request.Description ?? subSubCategory.Description;

            // Handle image update
            if (request.File != null)
            {
                try
                {
                    subSubCategory.ImageUrl = await _fileService.UpdateFileAsync(subSubCategory.ImageUrl, request.File, FileType.SubSubCategoryImages);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Image update failed:{ex.Message}");
                    return Result<SubSubCategoryDTO>.Failure("Image update failed");
                }
            }

            // Save changes to the database
            await _subSubCategoryRepository.UpdateAsync(subSubCategory, cancellationToken);
            await _subSubCategoryRepository.SaveChangesAsync(cancellationToken);

            // Map updated category to DTO
            var subSubCategoryDTO = new SubSubCategoryDTO
            {
                Id = subSubCategory.Id,
                Name = subSubCategory.Name,
                Slug = subSubCategory.Slug,
                Description = subSubCategory.Description,
                ImageUrl = subSubCategory.ImageUrl
            };

            return Result<SubSubCategoryDTO>.Success(subSubCategoryDTO, "SubCategory updated successfully");

        }
    }

}
