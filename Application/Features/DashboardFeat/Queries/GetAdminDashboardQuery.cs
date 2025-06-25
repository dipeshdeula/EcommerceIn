using Application.Common;
using Application.Dto.AdminDashboardDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Features.DashboardFeat.Queries
{
    public record GetAdminDashboardQuery : IRequest<Result<AdminDashboardDTO>>;

    public class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, Result<AdminDashboardDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public GetAdminDashboardQueryHandler(
            IUnitOfWork unitOfWork
            )
        {
            _unitOfWork = unitOfWork;
            
        }
        public async Task<Result<AdminDashboardDTO>> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
        {
            // USERS
            var totalUsers = await _unitOfWork.Users.CountAsync(null, cancellationToken);
            var today = DateTime.UtcNow.Date;
            var weekAgo = today.AddDays(-7);
            var monthAgo = today.AddMonths(-1);

            var newUsersToday = await _unitOfWork.Users.CountAsync(u => u.CreatedAt >= today, cancellationToken);
            var newUsersThisWeek = await _unitOfWork.Users.CountAsync(u => u.CreatedAt >= weekAgo, cancellationToken);
            var newUsersThisMonth = await _unitOfWork.Users.CountAsync(u => u.CreatedAt >= monthAgo, cancellationToken);

            // ORDERS
            var totalOrders = await _unitOfWork.Orders.CountAsync(null, cancellationToken);
            var ordersToday = await _unitOfWork.Orders.CountAsync(o => o.OrderDate >= today, cancellationToken);
            var ordersThisWeek = await _unitOfWork.Orders.CountAsync(o => o.OrderDate >= weekAgo, cancellationToken);
            var ordersThisMonth = await _unitOfWork.Orders.CountAsync(o => o.OrderDate >= monthAgo, cancellationToken);

            var orderStatusCounts = (await _unitOfWork.Orders.GetAllAsync(cancellationToken: cancellationToken))
                .GroupBy(o => o.OrderStatus)
                .ToDictionary(g => g.Key, g => g.Count());

            // SALES
            var allOrders = await _unitOfWork.Orders.GetAllAsync(
                cancellationToken:cancellationToken
                );
            decimal totalSales = allOrders.Sum(o => o.TotalAmount);
            decimal salesToday = allOrders.Where(o => o.OrderDate >= today).Sum(o => o.TotalAmount);
            decimal salesThisWeek = allOrders.Where(o => o.OrderDate >= weekAgo).Sum(o => o.TotalAmount);
            decimal salesThisMonth = allOrders.Where(o => o.OrderDate >= monthAgo).Sum(o => o.TotalAmount);

            // PRODUCTS
            var totalProducts = await _unitOfWork.Products.CountAsync(null, cancellationToken);
            var outOfStockProducts = await _unitOfWork.Products.CountAsync(p => p.StockQuantity <= 0, cancellationToken);
            var lowStockProducts = await _unitOfWork.Products.CountAsync(p => p.StockQuantity > 0 && p.StockQuantity <= 5, cancellationToken);

            // Top Selling Products (last 30 days)
            var orderItems = await _unitOfWork.OrderItems.GetAllAsync(
                predicate: oi => oi.Order.OrderDate >= monthAgo,
                includeProperties: "Product,Order",
                cancellationToken:cancellationToken
            );
            var topProducts = orderItems
                .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                .Select(g => new TopProductDTO
                {
                    ProductId = g.Key.ProductId,
                    Name = g.Key.Name,
                    SoldQuantity = g.Sum(x => x.Quantity),
                    TotalSales = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(tp => tp.SoldQuantity)
                .Take(5)
                .ToList();

            // PAYMENTS
            var payments = await _unitOfWork.PaymentRequests.GetAllAsync(cancellationToken: cancellationToken);
            var paymentMethodTotals = payments
                .GroupBy(p => p.PaymentMethodId)
                .ToDictionary(
                    g => g.Key.ToString(), // You may want to map ID to method name
                    g => g.Sum(x => x.PaymentAmount)
                );
            var failedPayments = payments.Count(p => p.PaymentStatus == "Failed");
            var successfulPayments = payments.Count(p => p.PaymentStatus == "Success" || p.PaymentStatus == "Paid");

            // RECENT ORDERS
            var recentOrders = allOrders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new RecentOrderDTO
                {
                    OrderId = o.Id,
                    UserName = o.User?.Name ?? "",
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
                    Name = u.Name,
                    Email = u.Email,
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

            // Get all products with their store
            var products = await _unitOfWork.Products.GetAllAsync(
                includeProperties: "ProductStores,ProductStores.Store",
                includeDeleted: false,
                cancellationToken: cancellationToken
            );

            // Get all order items with product and store info
            var storeOrderItems = await _unitOfWork.OrderItems.GetAllAsync(
                 includeProperties: "Product,Product.ProductStores,Product.ProductStores.Store,Order",
                 cancellationToken: cancellationToken
             );

            // Aggregate income per store
            var storeIncome = storeOrderItems
             .Where(oi => oi.Product?.ProductStores.Any() == true)
                .GroupBy(oi =>
                    oi.Product?.ProductStores.FirstOrDefault(ps => !ps.IsDeleted)?.StoreId
                )
             .Select(g =>
             {
                 var store = stores.FirstOrDefault(s => s.Id == g.Key);
                 return new TopStoreDTO
                 {
                     StoreId = store?.Id ?? 0,
                     StoreName = store?.Name ?? "",
                     OwnerName = store?.OwnerName ?? "",
                     ImageUrl = store?.ImageUrl ?? "",
                     TotalIncome = g.Sum(x => x.Quantity * x.UnitPrice)
                 };
             })
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

                // PAYMENTS
                PaymentMethodTotals = paymentMethodTotals,
                FailedPayments = failedPayments,
                SuccessfulPayments = successfulPayments,

                // RECENT ACTIVITY
                RecentOrders = recentOrders,
                RecentUsers = recentUsers,

                // Stores
                 TotalStores = totalStores,
                TopStoresByIncome = storeIncome
            };

            return Result<AdminDashboardDTO>.Success(dashboard, "Dashboard data fetched successfully");
        }
    }

}
