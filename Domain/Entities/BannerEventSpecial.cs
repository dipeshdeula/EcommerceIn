using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class BannerEventSpecial
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Offers { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } 
        public bool IsDeleted { get; set; }

        public ICollection<BannerImage> Images { get; set; } = new List<BannerImage>();

    }
}
