using Application.Common;
using Application.Dto;
using Application.Enums;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.BannerSpecialEvent.Commands
{
    public record UploadBannerImageCommand(
        int BannerId, IFormFileCollection Files) 
        : IRequest<Result<IEnumerable<BannerImageDTO>>>;

    public class UploadBannerImageCommandHandler : IRequestHandler<UploadBannerImageCommand, Result<IEnumerable<BannerImageDTO>>>
    {
        private readonly IBannerEventSpecialRepository _bannerEventSpecialRepository;
        private readonly IBannerImageRepository _bannerImageRepository;
        private readonly IFileServices _fileService;

        public UploadBannerImageCommandHandler(IBannerEventSpecialRepository bannerEventSpecialRepository, IBannerImageRepository bannerImageRepository, IFileServices fileService)
        {
            _bannerEventSpecialRepository = bannerEventSpecialRepository;
            _bannerImageRepository = bannerImageRepository;
            _fileService = fileService;
        
        }
  

        public async Task<Result<IEnumerable<BannerImageDTO>>> Handle(UploadBannerImageCommand request, CancellationToken cancellationToken)
        {
            //Validate BannerId
            var banner = await _bannerEventSpecialRepository.FindByIdAsync(request.BannerId);
            if (banner == null)
                return Result<IEnumerable<BannerImageDTO>>.Failure("BannerId not found");

            // Validate Files
            if (request.Files == null || request.Files.Count == 0)
            {
                return Result<IEnumerable<BannerImageDTO>>.Failure("No files uploaded. Please upload at least one image.");
            }

            var bannerImages = new List<BannerImage>();
            var bannerImageDTOs = new List<BannerImageDTO>();

            foreach(var file in request.Files)
            {
                // Save each file using the file service
                string fileUrl;
                try {
                    fileUrl = await _fileService.SaveFileAsync(file, FileType.BannerImages);

                }
                catch (Exception ex)
                {
                    return Result<IEnumerable<BannerImageDTO>>.Failure($"File upload failed: {ex.Message}");
                }

                //Create a new BannerImage entity
                var bannerImage = new BannerImage
                {
                    BannerId = request.BannerId,
                    ImageUrl = fileUrl
                };

                bannerImages.Add(bannerImage);
                bannerImageDTOs.Add(bannerImage.ToDTO());

            }

            // Save all BannerImages to the database
            await _bannerImageRepository.AddRangeAsync(bannerImages, cancellationToken);
            await _bannerImageRepository.SaveChangesAsync(cancellationToken);

            return Result<IEnumerable<BannerImageDTO>>.Success(bannerImageDTOs, "Banner images uploaded successfully");
        }
    }

}
