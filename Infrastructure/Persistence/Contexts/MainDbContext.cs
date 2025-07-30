using Domain.Entities.Common;
using Domain.Enums.BannerEventSpecial;
using Google;
using Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Contexts
{
    public class MainDbContext : DbContext
    {
        public MainDbContext(DbContextOptions<MainDbContext> options):base(options)
        {
            
        }
        //DbSet Properties
        public DbSet<User> Users { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<SubSubCategory> SubSubCategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<StoreAddress> StoreAddresses { get; set; }
        public DbSet<ProductStore> ProductStores { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<PaymentRequest> PaymentRequests { get; set; }
        public DbSet<Billing> Billings { get; set; }
        public DbSet<BillingItem> BillingItems { get; set; }
        public DbSet<BannerEventSpecial> BannerEventSpecials { get; set; }
        public DbSet<EventProduct> EventProducts { get; set; }
        public DbSet<EventUsage> EventUsages { get; set; }
        public DbSet<EventRule> EventRules { get; set; }
        public DbSet<BannerImage> BannerImages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<CompanyInfo> CompanyInfos { get; set; }

        public DbSet<Wishlist> Wishlists { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // configure all entitites
            builder.ApplyConfiguration(new UserConfig());
            builder.ApplyConfiguration(new AddressConfig());
            builder.ApplyConfiguration(new CategoryConfig());
            builder.ApplyConfiguration(new SubCategoryConfig());
            builder.ApplyConfiguration(new SubSubCategoryConfig());
            builder.ApplyConfiguration(new ProductConfig());
            builder.ApplyConfiguration(new ProductImageConfig());
            builder.ApplyConfiguration(new StoreConfig());
            builder.ApplyConfiguration(new ProductStoreConfig());
            builder.ApplyConfiguration(new OrderConfig());
            builder.ApplyConfiguration(new OrderItemConfig());
            builder.ApplyConfiguration(new CartItemConfig());
            builder.ApplyConfiguration(new RefreshTokenConfig());
            builder.ApplyConfiguration(new PaymentMethodConfig());
            builder.ApplyConfiguration(new PaymentRequestConfig());
            builder.ApplyConfiguration(new BillingConfig());
            builder.ApplyConfiguration(new BannerEventSpecialConfig());
            builder.ApplyConfiguration(new BannerImageConfig());
            builder.ApplyConfiguration(new EventProductConfig());
            builder.ApplyConfiguration(new EventRuleConfig());
            builder.ApplyConfiguration(new EventUsageConfig());
            builder.ApplyConfiguration(new NotificationConfig());
            builder.ApplyConfiguration(new CompanyInfoConfig());
            builder.ApplyConfiguration(new BillingConfig());
            builder.ApplyConfiguration(new WhishlistConfig());

           // builder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);

        }

     
    }
}
