using Application.Common;
using Application.Dto.AdminDashboardDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.DashboardFeat.Queries
{
    public record GetAdminDashboardQuery : IRequest<Result<AdminDashboardDTO>>;

    public class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, Result<AdminDashboardDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAdminDashboardQueryHandler> _logger;

        public GetAdminDashboardQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetAdminDashboardQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<AdminDashboardDTO>> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting admin dashboard data collection...");

                // USERS - Fix null predicate issue
                var totalUsers = await _unitOfWork.Users.CountAsync(u => true, cancellationToken);
                var today = DateTime.UtcNow.Date;
                var weekAgo = today.AddDays(-7);
                var monthAgo = today.AddMonths(-1);

                var newUsersToday = await _unitOfWork.Users.CountAsync(u => u.CreatedAt >= today, cancellationToken);
                var newUsersThisWeek = await _unitOfWork.Users.CountAsync(u => u.CreatedAt >= weekAgo, cancellationToken);
                var newUsersThisMonth = await _unitOfWork.Users.CountAsync(u => u.CreatedAt >= monthAgo, cancellationToken);

                // ORDERS - Fix null predicate issue
                var totalOrders = await _unitOfWork.Orders.CountAsync(o => true, cancellationToken); // Changed from null
                var ordersToday = await _unitOfWork.Orders.CountAsync(o => o.OrderDate >= today, cancellationToken);
                var ordersThisWeek = await _unitOfWork.Orders.CountAsync(o => o.OrderDate >= weekAgo, cancellationToken);
                var ordersThisMonth = await _unitOfWork.Orders.CountAsync(o => o.OrderDate >= monthAgo, cancellationToken);

                var orderStatusCounts = (await _unitOfWork.Orders.GetAllAsync(cancellationToken: cancellationToken))
                    .GroupBy(o => o.OrderStatus)
                    .ToDictionary(g => g.Key, g => g.Count());

                // SALES
                var allOrders = await _unitOfWork.Orders.GetAllAsync(
                    includeProperties: "User,User.Addresses,Items",
                    cancellationToken: cancellationToken
                );

                decimal totalSales = allOrders.Sum(o => o.TotalAmount);
                decimal salesToday = allOrders.Where(o => o.OrderDate >= today).Sum(o => o.TotalAmount);
                decimal salesThisWeek = allOrders.Where(o => o.OrderDate >= weekAgo).Sum(o => o.TotalAmount);
                decimal salesThisMonth = allOrders.Where(o => o.OrderDate >= monthAgo).Sum(o => o.TotalAmount);

                // PRODUCTS - Fix null predicate issue
                var totalProducts = await _unitOfWork.Products.CountAsync(p => true, cancellationToken); // Changed from null
                var outOfStockProducts = await _unitOfWork.Products.CountAsync(p => p.StockQuantity <= 0, cancellationToken);
                var lowStockProducts = await _unitOfWork.Products.CountAsync(p => p.StockQuantity > 0 && p.StockQuantity <= 5, cancellationToken);

                // Top Selling Products (last 30 days)
                var orderItems = await _unitOfWork.OrderItems.GetAllAsync(
                    predicate: oi => oi.Order.OrderDate >= monthAgo,
                    includeProperties: "Product,Order",
                    cancellationToken: cancellationToken
                );

                var topProducts = orderItems
                    .Where(oi => oi.Product != null)
                    .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                    .Select(g => new TopProductDTO
                    {
                        ProductId = g.Key.ProductId,
                        Name = g.Key.Name ?? "Unknown Product",
                        SoldQuantity = g.Sum(x => x.Quantity),
                        TotalSales = g.Sum(x => x.Quantity * x.UnitPrice)
                    })
                    .OrderByDescending(tp => tp.SoldQuantity)
                    .Take(5)
                    .ToList();

                // CATEGORIES
                var totalCategories = await _unitOfWork.Categories.CountAsync(c => !c.IsDeleted, cancellationToken);
                var totalSubCategories = await _unitOfWork.SubCategories.CountAsync(sc => !sc.IsDeleted, cancellationToken);
                var totalSubSubCategories = await _unitOfWork.SubSubCategories.CountAsync(ssc => !ssc.IsDeleted, cancellationToken);

                // Get categories with products for statistics
                var categoriesWithProducts = await _unitOfWork.Categories.GetAllAsync(
                    predicate: c => !c.IsDeleted,
                    includeProperties: "SubCategories.SubSubCategories.Products",
                    cancellationToken: cancellationToken);

                var categoriesWithActiveProducts = categoriesWithProducts
                    .Where(c => c.SubCategories.Any(sc => 
                        sc.SubSubCategories.Any(ssc => 
                            ssc.Products.Any(p => !p.IsDeleted))))
                    .Count();

                var emptyCategories = totalCategories - categoriesWithActiveProducts;

                // PAYMENTS
                var payments = await _unitOfWork.PaymentRequests.GetAllAsync(cancellationToken: cancellationToken);
                var paymentMethodTotals = payments                   
                    .GroupBy(p => p.PaymentMethodId)
                    .ToDictionary(
                        g => g.Key.ToString() ?? "Unknown",
                        g => g.Sum(x => x.PaymentAmount)
                    );

                var failedPayments = payments.Count(p => p.PaymentStatus == "Failed");
                var successfulPayments = payments.Count(p => p.PaymentStatus == "Succeeded" || p.PaymentStatus == "Paid");

                // RECENT ORDERS
                var recentOrders = allOrders
                    .Where(o => o.User != null) // Add null check
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .Select(o => new RecentOrderDTO
                    {
                        OrderId = o.Id,
                        UserName = o.User?.Name ?? "Unknown User",
                        Amount = o.TotalAmount,
                        Status = o.OrderStatus,
                        OrderDate = o.OrderDate
                    })
                    .ToList();

                // RECENT USERS
                var recentUsers = (await _unitOfWork.Users.GetAllAsync(
                        orderBy: q => q.OrderByDescending(u => u.CreatedAt),
                        take: 5,
                        cancellationToken: cancellationToken
                    ))
                    .Select(u => new RecentUserDTO
                    {
                        UserId = u.Id,
                        Name = u.Name ?? "Unknown",
                        Email = u.Email ?? "No Email",
                        RegisteredAt = u.CreatedAt
                    })
                    .ToList();

                // STORES
                var stores = await _unitOfWork.Stores.GetAllAsync(
                    predicate: s => !s.IsDeleted,
                    includeProperties: "Address",
                    cancellationToken: cancellationToken
                );

                var totalStores = stores.Count();

                // Get all products with their store relationships
                var products = await _unitOfWork.Products.GetAllAsync(
                    predicate: p => !p.IsDeleted, // Add proper predicate instead of using includeDeleted parameter
                    includeProperties: "ProductStores,ProductStores.Store",
                    cancellationToken: cancellationToken
                );

                // Get all order items with product and store info
                var storeOrderItems = await _unitOfWork.OrderItems.GetAllAsync(
                    includeProperties: "Product,Product.ProductStores,Product.ProductStores.Store,Order",
                    cancellationToken: cancellationToken
                );

                // Aggregate income per store with null safety
                var storeIncome = storeOrderItems
                    .Where(oi => oi.Product?.ProductStores?.Any(ps => !ps.IsDeleted) == true)
                    .GroupBy(oi => oi.Product.ProductStores.FirstOrDefault(ps => !ps.IsDeleted)?.StoreId)
                    .Where(g => g.Key.HasValue) // Filter out null store IDs
                    .Select(g =>
                    {
                        var store = stores.FirstOrDefault(s => s.Id == g.Key);
                        return new TopStoreDTO
                        {
                            StoreId = store?.Id ?? 0,
                            StoreName = store?.Name ?? "Unknown Store",
                            OwnerName = store?.OwnerName ?? "Unknown Owner",
                            ImageUrl = store?.ImageUrl ?? "",
                            TotalIncome = g.Sum(x => x.Quantity * x.UnitPrice),
                            OrderCount = g.Select(x => x.OrderId).Distinct().Count(),
                            ProductCount = g.Select(x => x.ProductId).Distinct().Count()
                        };
                    })
                    .Where(store => store.StoreId > 0) // Filter out invalid stores
                    .OrderByDescending(ts => ts.TotalIncome)
                    .Take(5)
                    .ToList();

                var dashboard = new AdminDashboardDTO
                {
                    // SALES
                    TotalSales = totalSales,
                    SalesToday = salesToday,
                    SalesThisWeek = salesThisWeek,
                    SalesThisMonth = salesThisMonth,

                    // ORDERS
                    TotalOrders = totalOrders,
                    OrdersToday = ordersToday,
                    OrdersThisWeek = ordersThisWeek,
                    OrdersThisMonth = ordersThisMonth,
                    OrderStatusCounts = orderStatusCounts,

                    // USERS
                    TotalUsers = totalUsers,
                    NewUsersToday = newUsersToday,
                    NewUsersThisWeek = newUsersThisWeek,
                    NewUsersThisMonth = newUsersThisMonth,

                    // PRODUCTS
                    TotalProducts = totalProducts,
                    OutOfStockProducts = outOfStockProducts,
                    LowStockProducts = lowStockProducts,
                    TopSellingProducts = topProducts,


                    // CATEGORIES
                    TotalCategories = totalCategories,
                    TotalSubCategories = totalSubCategories,
                    TotalSubSubCategories = totalSubSubCategories,
                    CategoriesWithProducts = categoriesWithActiveProducts,
                    EmptyCategories = emptyCategories,

                 


                    // PAYMENTS
                    PaymentMethodTotals = paymentMethodTotals,
                    FailedPayments = failedPayments,
                    SuccessfulPayments = successfulPayments,



                    // RECENT ACTIVITY
                    RecentOrders = recentOrders,
                    RecentUsers = recentUsers,

                    // STORES
                    TotalStores = totalStores,
                    TopStoresByIncome = storeIncome
                };

                _logger.LogInformation("Admin dashboard data collection completed successfully");
                return Result<AdminDashboardDTO>.Success(dashboard, "Dashboard data fetched successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin dashboard data");
                return Result<AdminDashboardDTO>.Failure($"Failed to retrieve dashboard data: {ex.Message}");
            }
        }
    }
}