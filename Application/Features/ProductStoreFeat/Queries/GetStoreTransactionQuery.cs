using Application.Common;
using Application.Dto.StoreDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.ProductStoreFeat.Queries
{
    public record GetStoreTransactionsQuery(
        int StoreId,
        DateTime? FromDate = null,
        DateTime? ToDate = null,
        int PageNumber = 1,
        int PageSize = 20,
        string? OrderStatus = null,
        bool IncludeDeleted = false,
        bool IsAdminRequest = false
    ) : IRequest<Result<StoreTransactionDTO>>;

    public class GetStoreTransactionsQueryHandler : IRequestHandler<GetStoreTransactionsQuery, Result<StoreTransactionDTO>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IStoreRepository _storeRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetStoreTransactionsQueryHandler> _logger;

        public GetStoreTransactionsQueryHandler(
            IOrderRepository orderRepository,
            IStoreRepository storeRepository,
            ICurrentUserService currentUserService,
            ILogger<GetStoreTransactionsQueryHandler> logger)
        {
            _orderRepository = orderRepository;
            _storeRepository = storeRepository;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<StoreTransactionDTO>> Handle(GetStoreTransactionsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching transaction details for Store {StoreId} from {FromDate} to {ToDate}",
                    request.StoreId, request.FromDate, request.ToDate);

                //  STEP 1: Validate store exists
                var store = await _storeRepository.FindByIdAsync(request.StoreId);
                if (store == null || (!request.IncludeDeleted && store.IsDeleted))
                {
                    return Result<StoreTransactionDTO>.Failure("Store not found or unavailable");
                }

                //  STEP 2: Set date range (default to last 30 days if not specified)
                var fromDate = request.FromDate ?? DateTime.UtcNow.AddDays(-30);
                var toDate = request.ToDate ?? DateTime.UtcNow;

                if (fromDate > toDate)
                {
                    return Result<StoreTransactionDTO>.Failure("From date cannot be greater than to date");
                }

                // STEP 3: Get all order items for the store within date range
                var orderItems = await _orderRepository.GetQueryable()
                    .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
                    .Where(o => !request.IncludeDeleted ? !o.IsDeleted : true)
                    .SelectMany(o => o.Items) 
                    .Where(oi => oi.Product!.ProductStores.Any(ps => ps.StoreId == request.StoreId &&
                                (!request.IncludeDeleted ? !ps.IsDeleted : true)))
                    .Include(oi => oi.Order)
                        .ThenInclude(o => o!.User)
                    .Include(oi => oi.Product)
                        .ThenInclude(p => p!.Images.Where(img => !img.IsDeleted))
                    .Include(oi => oi.Product)
                        .ThenInclude(p => p!.ProductStores.Where(ps => ps.StoreId == request.StoreId))
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                // STEP 4: Apply order status filter if specified
                if (!string.IsNullOrEmpty(request.OrderStatus))
                {
                    orderItems = orderItems
                        .Where(oi => oi.Order!.OrderStatus.Equals(request.OrderStatus, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // STEP 5: Apply pagination to order items
                var totalItems = orderItems.Count;
                var paginatedOrderItems = orderItems
                    .OrderByDescending(oi => oi.Order!.OrderDate)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // STEP 6: Calculate transaction details with proper property mappings
                var transactionDetails = paginatedOrderItems.Select(oi => new StoreTransactionDetailDTO
                {
                    OrderId = oi.OrderId,
                    OrderDate = oi.Order!.OrderDate,
                    CustomerName = !string.IsNullOrEmpty(oi.Order.User?.Name) 
                        ? oi.Order.User.Name 
                        : oi.Order.User?.Email?.Split('@')[0] ?? "Unknown Customer",
                    CustomerEmail = oi.Order.User?.Email ?? "No Email",

                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? oi.ProductName ?? "Unknown Product",
                    ProductSku = oi.Product?.Sku ?? "N/A",
                    ProductImageUrl = oi.Product?.Images?.FirstOrDefault(img => img.IsMain)?.ImageUrl ?? string.Empty,

                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice,
                    
                    //  Separate discount tracking
                    EventDiscountApplied = oi.EventDiscountAmount ?? 0,
                    RegularDiscountApplied = oi.RegularDiscountAmount ?? 0,
                    DiscountApplied = (oi.EventDiscountAmount ?? 0) + (oi.RegularDiscountAmount ?? 0),
                    NetAmount = oi.TotalPrice - ((oi.EventDiscountAmount ?? 0) + (oi.RegularDiscountAmount ?? 0)),

                    OrderStatus = oi.Order.OrderStatus ?? "Unknown",
                    PaymentStatus = oi.Order.PaymentStatus ?? "Unknown",
                    DeliveredDate = oi.Order.OrderStatus?.ToLower() == "delivered" ? oi.Order.UpdatedAt : null,

                    HasDiscount = ((oi.EventDiscountAmount ?? 0) + (oi.RegularDiscountAmount ?? 0)) > 0,
                    HasEventDiscount = (oi.EventDiscountAmount ?? 0) > 0,
                    HasRegularDiscount = (oi.RegularDiscountAmount ?? 0) > 0,
                    AppliedEventId = oi.AppliedEventId,
                    
                    // Formatted values
                    FormattedUnitPrice = $"Rs.{oi.UnitPrice:F2}",
                    FormattedTotalPrice = $"Rs.{oi.TotalPrice:F2}",
                    FormattedNetAmount = $"Rs.{oi.TotalPrice - ((oi.EventDiscountAmount ?? 0) + (oi.RegularDiscountAmount ?? 0)):F2}",
                    FormattedEventDiscount = $"Rs.{oi.EventDiscountAmount ?? 0:F2}",
                    FormattedRegularDiscount = $"Rs.{oi.RegularDiscountAmount ?? 0:F2}"
                }).ToList();

                // STEP 7: Calculate summary statistics with proper discount calculation
                var totalRevenue = orderItems.Sum(oi => oi.TotalPrice - ((oi.EventDiscountAmount ?? 0) + (oi.RegularDiscountAmount ?? 0)));
                var totalProductsSold = orderItems.Sum(oi => oi.Quantity);
                var uniqueOrders = orderItems.Select(oi => oi.OrderId).Distinct().Count();
                var averageOrderValue = uniqueOrders > 0 ? totalRevenue / uniqueOrders : 0;

                // STEP 8: Create comprehensive statistics with proper property mappings
                var statistics = new Dictionary<string, object>
                {
                    ["TotalPages"] = (int)Math.Ceiling((double)totalItems / request.PageSize),
                    ["CurrentPage"] = request.PageNumber,
                    ["PageSize"] = request.PageSize,
                    ["HasNextPage"] = request.PageNumber < Math.Ceiling((double)totalItems / request.PageSize),
                    ["HasPreviousPage"] = request.PageNumber > 1,

                    ["TopSellingProduct"] = orderItems
                        .GroupBy(oi => new { oi.ProductId, ProductName = oi.Product?.Name ?? oi.ProductName })
                        .OrderByDescending(g => g.Sum(oi => oi.Quantity))
                        .FirstOrDefault()?.Key.ProductName ?? "N/A",

                    ["HighestValueOrder"] = orderItems.Any()
                        ? orderItems
                            .GroupBy(oi => oi.OrderId)
                            .Max(g => g.Sum(oi => oi.TotalPrice - ((oi.EventDiscountAmount ?? 0) + (oi.RegularDiscountAmount ?? 0))))
                        : 0,

                    ["AverageQuantityPerOrder"] = uniqueOrders > 0 ? (double)totalProductsSold / uniqueOrders : 0,

                    //  Calculate total discount from both sources
                    ["DiscountGiven"] = orderItems.Sum(oi => (oi.EventDiscountAmount ?? 0) + (oi.RegularDiscountAmount ?? 0)),

                    ["OrderStatusBreakdown"] = orderItems
                        .GroupBy(oi => oi.Order!.OrderStatus) 
                        .ToDictionary(g => g.Key ?? "Unknown", g => g.Count()),

                    ["DailyRevenue"] = orderItems
                        .GroupBy(oi => oi.Order!.OrderDate.Date)
                        .OrderBy(g => g.Key)
                        .ToDictionary(
                            g => g.Key.ToString("yyyy-MM-dd"),
                            g => g.Sum(oi => oi.TotalPrice - ((oi.EventDiscountAmount ?? 0) + (oi.RegularDiscountAmount ?? 0)))
                        ),

                    // Additional insights
                    ["EventDiscountGiven"] = orderItems.Sum(oi => oi.EventDiscountAmount ?? 0),
                    ["RegularDiscountGiven"] = orderItems.Sum(oi => oi.RegularDiscountAmount ?? 0),
                    ["OrdersWithEventDiscount"] = orderItems.Count(oi => (oi.EventDiscountAmount ?? 0) > 0),
                    ["OrdersWithRegularDiscount"] = orderItems.Count(oi => (oi.RegularDiscountAmount ?? 0) > 0),
                    
                    ["AverageOrderItemValue"] = orderItems.Any() 
                        ? orderItems.Average(oi => oi.TotalPrice - ((oi.EventDiscountAmount ?? 0) + (oi.RegularDiscountAmount ?? 0)))
                        : 0
                };

                //  Create final transaction DTO
                var storeTransaction = new StoreTransactionDTO
                {
                    StoreId = store.Id,
                    StoreName = store.Name,
                    OwnerName = store.OwnerName,
                    StoreAddress = FormatStoreAddress(store.Address),
                    FromDate = fromDate,
                    ToDate = toDate,

                    TotalOrders = uniqueOrders,
                    TotalProductsSold = totalProductsSold,
                    TotalRevenue = totalRevenue,
                    AverageOrderValue = averageOrderValue,

                    TransactionDetails = transactionDetails,
                    Statistics = statistics
                };

                _logger.LogInformation("Retrieved {TransactionCount} transactions for Store {StoreId} - Revenue: Rs.{Revenue:F2}, Products Sold: {ProductsSold}",
                    transactionDetails.Count, request.StoreId, totalRevenue, totalProductsSold);

                var message = request.IsAdminRequest
                    ? $"Store transactions retrieved (Admin view). Total revenue: Rs.{totalRevenue:F2} from {uniqueOrders} orders"
                    : $"Store transactions retrieved. Total revenue: Rs.{totalRevenue:F2} from {uniqueOrders} orders";

                return Result<StoreTransactionDTO>.Success(storeTransaction, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transactions for Store {StoreId}", request.StoreId);
                return Result<StoreTransactionDTO>.Failure($"Failed to retrieve store transactions: {ex.Message}");
            }
        }

        private static string FormatStoreAddress(StoreAddress? address)
        {
            if (address == null) return "Address not available";

            var parts = new List<string>();
            if (!string.IsNullOrEmpty(address.Street)) parts.Add(address.Street);
            if (!string.IsNullOrEmpty(address.City)) parts.Add(address.City);
            if (!string.IsNullOrEmpty(address.Province)) parts.Add(address.Province);
            if (!string.IsNullOrEmpty(address.PostalCode)) parts.Add(address.PostalCode);

            return parts.Any() ? string.Join(", ", parts) : "Address not available";
        }
    }
}