using Application.Common;
using Application.Dto.BannerEventSpecialDTOs;
using Application.Extension;
using Application.Interfaces.Services;
using Domain.Enums.BannerEventSpecial;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.BannerSpecialEvent.Queries
{
    public record GetActiveBannerEventsQuery(

        ) : IRequest<Result<IEnumerable<BannerEventSpecialDTO>>>;

    public class GetActiveBannerEventsQueryHandler : IRequestHandler<GetActiveBannerEventsQuery, Result<IEnumerable<BannerEventSpecialDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetActiveBannerEventsQueryHandler> _logger;

        public GetActiveBannerEventsQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetActiveBannerEventsQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<BannerEventSpecialDTO>>> Handle(GetActiveBannerEventsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var now = DateTime.UtcNow;

                var activeEvents = await _unitOfWork.BannerEventSpecials.GetAllAsync(
                    predicate: e => e.IsActive &&
                                   !e.IsDeleted &&
                                   e.Status == EventStatus.Active &&
                                   e.StartDate <= now &&
                                   e.EndDate >= now,
                    orderBy: q => q.OrderByDescending(e => e.Priority)
                                   .ThenByDescending(e => e.CreatedAt),
                    take: 50, // Limit for performance
                    includeProperties: "Images",
                    includeDeleted: false, cancellationToken: cancellationToken);

                var dtos = activeEvents.Select(e =>
                {
                    var dto = e.ToDTO();
                    // Only include basic info for public endpoint
                    dto.Rules = new List<EventRuleDTO>(); // Clear sensitive data
                    dto.EventProducts = new List<EventProductDTO>(); // Clear sensitive data
                    return dto;
                }).ToList();

                _logger.LogInformation("Retrieved {Count} active banner events", dtos.Count);

                return Result<IEnumerable<BannerEventSpecialDTO>>.Success(dtos,
                    $"Retrieved {dtos.Count} active banner events");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve active banner events");
                return Result<IEnumerable<BannerEventSpecialDTO>>.Failure($"Failed to retrieve active banner events: {ex.Message}");
            }
        }
    }
}
