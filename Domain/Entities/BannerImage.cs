using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class BannerImage
    {
        public int Id { get; set; }
        public int BannerId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsMain { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        public BannerEventSpecial BannerEventSpecial { get; set; }
    }
}
