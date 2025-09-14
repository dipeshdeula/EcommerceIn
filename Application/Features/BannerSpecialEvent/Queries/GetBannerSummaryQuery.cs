using Application.Common;
using Application.Common.Models;
using Application.Dto.BannerEventSpecialDTOs;
using Application.Extension;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Application.Features.BannerSpecialEvent.Queries
{
    public record GetBannerSummaryQuery(
        int PageNumber,
        int PageSize,
        bool IncludeDeleted = false) : IRequest<Result<PagedResult<BannerSummaryDTO>>>;


    public class GetBannerSummaryQueryHandler : IRequestHandler<GetBannerSummaryQuery, Result<PagedResult<BannerSummaryDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetBannerSummaryQueryHandler> _logger;
        private readonly INepalTimeZoneService _nepalTimeZoneService;
        public GetBannerSummaryQueryHandler(
            IUnitOfWork unitOfWOrk,
            ILogger<GetBannerSummaryQueryHandler> logger,
            INepalTimeZoneService nepalTimeZoneService
            )
        {
            _unitOfWork = unitOfWOrk;
            _logger = logger;
            _nepalTimeZoneService = nepalTimeZoneService;
        }
        public async Task<Result<PagedResult<BannerSummaryDTO>>> Handle(GetBannerSummaryQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Build predicate for filtering
                Expression<Func<BannerEventSpecial, bool>> predicate = e => true;

                if (!request.IncludeDeleted)
                {
                    predicate = e => !e.IsDeleted;
                }

                // Get total count for pagination
                var totalCount = await _unitOfWork.BannerEventSpecials.CountAsync(predicate, cancellationToken);

                // Get banner events with all related data
                var bannerEvents = await _unitOfWork.BannerEventSpecials.GetAllAsync(
                    predicate: predicate,
                    orderBy: query => query.OrderByDescending(e => e.CreatedAt)
                                          .ThenByDescending(e => e.Priority),
                    skip: (request.PageNumber - 1) * request.PageSize,
                    take: request.PageSize,
                    includeProperties: "Images",
                    includeDeleted: request.IncludeDeleted);

                // Convert to DTOs with enhanced analytics data
                var bannerEventDTOs = new List<BannerSummaryDTO>();

                foreach (var bannerEvent in bannerEvents)
                {


                    var dto = bannerEvent.ToDTO(_nepalTimeZoneService);
                    bannerEventDTOs.Add(new BannerSummaryDTO { 
                        Id = dto.Id,
                        Name = dto.Name,
                        Description = dto.Description,
                        TagLine = dto.TagLine,
                        DiscountValue = dto.DiscountValue,
                        PromotionType = dto.PromotionType,
                        MaxDiscountAmount = dto.MaxDiscountAmount,
                        StartDateNepal = dto.StartDateNepal,
                        EndDateNepal = dto.EndDateNepal,
                        IsActive = dto.IsActive,
                        IsExpired = dto.IsExpired,
                        DaysRemaining = dto.DaysRemaining,
                        Images = dto.Images
                    });
                }

                // Create paged result
                var pagedResult = new PagedResult<BannerSummaryDTO>
                {
                    Data = bannerEventDTOs,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
                };

                _logger.LogInformation("Retrieved {Count} banner events with analytics data (Page {PageNumber}/{TotalPages})",
                    bannerEventDTOs.Count, request.PageNumber, pagedResult.TotalPages);

                return Result<PagedResult<BannerSummaryDTO>>.Success(
                    pagedResult,
                    $"Banner events retrieved successfully with analytics. Page {request.PageNumber} of {pagedResult.TotalPages}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve banner events for page {PageNumber}", request.PageNumber);
                return Result<PagedResult<BannerSummaryDTO>>.Failure($"Failed to retrieve banner events: {ex.Message}");
            }

        }

    }
}


