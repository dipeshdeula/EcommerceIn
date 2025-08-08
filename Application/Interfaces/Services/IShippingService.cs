using Application.Common;
using Application.Dto.ShippingDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IShippingService
    {
        // Shipping Calculation
        Task<Result<ShippingCalculationDetailDTO>> CalculateShippingAsync(
           ShippingRequestDTO request, 
            CancellationToken cancellationToken = default);

        //  Management (Admin)
        Task<Result<List<ShippingDTO>>> GetAllsAsync(
            CancellationToken cancellationToken = default);
        
        Task<Result<ShippingDTO>> GetByIdAsync(
            int Id, 
            CancellationToken cancellationToken = default);
        
        Task<Result<ShippingDTO>> GetActiveAsync(
            CancellationToken cancellationToken = default);
        
        Task<Result<ShippingDTO>> CreateAsync(
            CreateShippingDTO request, 
            int createdByUserId,
            CancellationToken cancellationToken = default);
        
        Task<Result<ShippingDTO>> UpdateAsync(
            int Id, 
            CreateShippingDTO request, 
            int modifiedByUserId,
            CancellationToken cancellationToken = default);
        
        Task<Result<bool>> SetDefaultAsync(
            int Id, 
            int modifiedByUserId,
            CancellationToken cancellationToken = default);
        
        Task<Result<bool>> ActivateAsync(
            int Id, 
            int modifiedByUserId,
            CancellationToken cancellationToken = default);
        
        Task<Result<bool>> DeactivateAsync(
            int Id, 
            int modifiedByUserId,
            CancellationToken cancellationToken = default);

        // Promotion Management
        Task<Result<bool>> EnableFreeShippingPromotionAsync(
            int Id,
            DateTime startDate,
            DateTime endDate,
            string description,
            int modifiedByUserId,
            CancellationToken cancellationToken = default);
        
        Task<Result<bool>> DisableFreeShippingPromotionAsync(
            int Id,
            int modifiedByUserId,
            CancellationToken cancellationToken = default);

        // Public API for Frontend
        Task<Result<object>> GetShippingInfoForCustomersAsync(
            CancellationToken cancellationToken = default);
    }
}
