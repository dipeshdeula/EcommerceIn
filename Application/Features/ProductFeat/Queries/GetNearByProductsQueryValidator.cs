using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.ProductFeat.Queries
{
    public class GetNearbyProductsQueryValidator : AbstractValidator<GetNearbyProductsQuery>
    {
        public GetNearbyProductsQueryValidator()
        {
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.");

            RuleFor(x => x.RadiusKm)
                .GreaterThan(0).WithMessage("Radius must be greater than 0 km.");

            RuleFor(x => x.skip)
                .GreaterThanOrEqualTo(0).WithMessage("Skip must be 0 or greater.");

            RuleFor(x => x.take)
                .GreaterThan(0).WithMessage("Take must be greater than 0.");
        }
    }
}
