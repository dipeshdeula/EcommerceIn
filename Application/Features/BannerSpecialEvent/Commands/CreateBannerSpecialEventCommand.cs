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
    public record CreateBannerSpecialEventCommand
        (
            
            string Name,
            string Description,
            double Offers,
            DateTime StartDate,
            DateTime EndDate
     

        ) : IRequest<Result<BannerEventSpecialDTO>>;

    public class CreateBannerSpecialEventCommandHandler : IRequestHandler<CreateBannerSpecialEventCommand, Result<BannerEventSpecialDTO>>
    {
        private readonly IBannerEventSpecialRepository _bannerSpecialEventRepository;
        private readonly IFileServices _fileService;
        public CreateBannerSpecialEventCommandHandler(IBannerEventSpecialRepository bannerSpecialEventRepository,IFileServices fileService)
        {
            _bannerSpecialEventRepository = bannerSpecialEventRepository;
            _fileService = fileService;
            
        }
        public async Task<Result<BannerEventSpecialDTO>> Handle(CreateBannerSpecialEventCommand request, CancellationToken cancellationToken)
        {
           

            // Create the new BannerEvents
            var bannerEvent = new BannerEventSpecial
            {
                Name = request.Name,
                Description = request.Description,
                Offers = Convert.ToDouble(request.Offers),
                StartDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc),
                
            };

            var createBannerEvent = await _bannerSpecialEventRepository.AddAsync(bannerEvent);

            if (createBannerEvent == null)
                return Result<BannerEventSpecialDTO>.Failure("Failed to create banner special event");

            // Map to DTO 
            return Result<BannerEventSpecialDTO>.Success(createBannerEvent.ToDTO(), "Banner special event created successfully");
        }
    }

}
