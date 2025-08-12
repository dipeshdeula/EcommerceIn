using Application.Common;
using Application.Common.Models;
using Application.Dto.PromoCodeDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.PromoCodeFeat.Queries
{
    public record GetAllPromoCodesQuery(
        bool IncludeInactive = false,
        bool IncludeExpired = false,
        int? CategoryId = null,
        int PageNumber = 1,
        int PageSize = 20
    ) : IRequest<Result<PagedResult<PromoCodeDTO>>>;
    
    public class GetAllPromoCodesQueryHandler : IRequestHandler<GetAllPromoCodesQuery, Result<PagedResult<PromoCodeDTO>>>
    {
        private readonly IPromoCodeRepository _promoCodeRepository;
        private readonly INepalTimeZoneService _nepalTimeZoneService;
        private readonly ILogger<GetAllPromoCodesQueryHandler> _logger;
        
        public GetAllPromoCodesQueryHandler(
            IPromoCodeRepository promoCodeRepository,
            INepalTimeZoneService nepalTimeZoneService,
            ILogger<GetAllPromoCodesQueryHandler> logger)
        {
            _promoCodeRepository = promoCodeRepository;
            _nepalTimeZoneService = nepalTimeZoneService;
            _logger = logger;
        }
        
        public async Task<Result<PagedResult<PromoCodeDTO>>> Handle(GetAllPromoCodesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Build predicate for filtering
                var predicate = BuildPredicate(request);
                
                // Get total count for pagination
                //var totalCount = await _promoCodeRepository.CountAsync(
                //    predicate: predicate,
                //    cancellationToken: cancellationToken
                //);
                
                // Get promo codes with proper pagination
                var promoCodes = await _promoCodeRepository.GetAllAsync(
                    //predicate: predicate,
                    orderBy: q => q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Name),
                    skip: (request.PageNumber-1) * request.PageSize,
                    take: request.PageSize,
                    includeProperties:"CreatedByUser,LastModifiedByUser,Category",
                    cancellationToken:cancellationToken
                   
                );

                var totalCount = await _promoCodeRepository.CountAsync(                  
                   cancellationToken: cancellationToken);


                // Convert to DTOs
               var promoCodeDTOs = promoCodes.Select(p => p.ToPromoCodeDTO(_nepalTimeZoneService)).ToList();
                
                // Create paged result using YOUR existing pattern
                var result = new PagedResult<PromoCodeDTO>
                {
                    Data = promoCodeDTOs,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
                };
                
                _logger.LogInformation("Retrieved {Count} promo codes (Page {PageNumber}/{TotalPages})", 
                    promoCodeDTOs.Count, request.PageNumber, result.TotalPages);
                
                return Result<PagedResult<PromoCodeDTO>>.Success(result, "Promo codes retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting promo codes");
                return Result<PagedResult<PromoCodeDTO>>.Failure($"Error retrieving promo codes: {ex.Message}");
            }
        }
        
        private static System.Linq.Expressions.Expression<Func<Domain.Entities.PromoCode, bool>> BuildPredicate(GetAllPromoCodesQuery request)
        {
            return p => !p.IsDeleted &&
                       (request.IncludeInactive || p.IsActive) &&
                       (request.IncludeExpired || p.EndDate >= DateTime.UtcNow) &&
                       (!request.CategoryId.HasValue || p.CategoryId == request.CategoryId);
        }
    }
}