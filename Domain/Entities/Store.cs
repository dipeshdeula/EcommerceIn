using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    // Stores for Multi-vendor or Location-based Products)
    public class Store
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }

        // location integration
        public int? ServiceAreaId { get; set; }
        public bool IsLocationVerified { get; set; } = false;
        public StoreAddress? Address { get; set; }
        public ServiceArea? ServiceArea { get; set; }
        public ICollection<ProductStore> ProductStores { get; set; } = new List<ProductStore>();

    }
}
