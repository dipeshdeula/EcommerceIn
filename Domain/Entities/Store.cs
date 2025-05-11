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
        public string ImageUrl { get; set; }
        public bool IsDeleted { get; set; }
        public StoreAddress Address { get; set; }
        public ICollection<ProductStore> ProductStores { get; set; } = new List<ProductStore>();

    }
}
