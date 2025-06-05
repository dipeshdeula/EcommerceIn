using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums.BannerEventSpecial
{
    public enum EventType
    {
        Seasonal,    // Summer, Winter sales
        Festive,     // Christmas, Diwali, Eid
        Occasional,  // Back to school, Valentine's
        Flash,       // Limited time offers
        Clearance,   // End of season
        NewArrival,  // Launch promotions
        Loyalty      // Member exclusive
    }
}
