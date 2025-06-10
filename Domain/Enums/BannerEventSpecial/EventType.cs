using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums.BannerEventSpecial
{
    public enum EventType
    {
        Seasonal = 0,    // Summer, Winter sales
        Festive = 1,     // Christmas, Diwali, Eid
        Occasional = 2,  // Back to school, Valentine's
        Flash = 3,       // Limited time offers
        Clearance = 4,   // End of season
        NewArrival = 5,  // Launch promotions
        Loyalty = 6      // Member exclusive
    }
}
