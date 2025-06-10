using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums.BannerEventSpecial
{
    public enum EventStatus
    {
        Draft = 0,
        Scheduled = 1,
        Active = 2,
        Paused = 3,
        Expired = 4,
        Cancelled = 5
    }
}
