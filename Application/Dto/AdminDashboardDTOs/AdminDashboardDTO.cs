using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.AdminDashboardDTOs
{
    public class AdminDashboardDTO
    {
        // SALES
        public decimal TotalSales { get; set; }
        public decimal SalesToday { get; set; }
        public decimal SalesThisWeek { get; set; }
        public decimal SalesThisMonth { get; set; }
        public int TotalOrders { get; set; }
        public int OrdersToday { get; set; }
        public int OrdersThisWeek { get; set; }
        public int OrdersThisMonth { get; set; }
        public Dictionary<string, int> OrderStatusCounts { get; set; } = new(); // e.g. { "Pending": 10, "Completed": 50 }

        // USERS
        public int TotalUsers { get; set; }
        public int NewUsersToday { get; set; }
        public int NewUsersThisWeek { get; set; }
        public int NewUsersThisMonth { get; set; }

        // PRODUCTS
        public int TotalProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public List<TopProductDTO> TopSellingProducts { get; set; } = new();

           // CATEGORY STATISTICS
        public int TotalCategories { get; set; }
        public int TotalSubCategories { get; set; }
        public int TotalSubSubCategories { get; set; }
        public int CategoriesWithProducts { get; set; }
        public int EmptyCategories { get; set; }

        // PAYMENTS
        public Dictionary<string, decimal> PaymentMethodTotals { get; set; } = new(); // e.g. { "COD": 10000, "eSewa": 5000 }
        public int FailedPayments { get; set; }
        public int SuccessfulPayments { get; set; }

        // RECENT ACTIVITY
        public List<RecentOrderDTO> RecentOrders { get; set; } = new();
        public List<RecentUserDTO> RecentUsers { get; set; } = new();

        // Stores
        public int TotalStores { get; set; }
        public List<TopStoreDTO> TopStoresByIncome { get; set; } = new List<TopStoreDTO>();
    }
}
