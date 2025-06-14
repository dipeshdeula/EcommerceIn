using Application.Features.BannerSpecialEvent.Queries;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.BannerSpecialEvent.Validators
{
    public class GetAllBannerEventSpecialQueryValidator : AbstractValidator<GetAllBannerEventSpecialQuery>
    {
        public GetAllBannerEventSpecialQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100");

            RuleFor(x => x.Status)
                .Must(BeValidEventStatus)
                .When(x => !string.IsNullOrEmpty(x.Status))
                .WithMessage("Invalid event status. Valid values are: Draft, Active, Paused, Expired, Cancelled");
        }

        private bool BeValidEventStatus(string? status)
        {
            if (string.IsNullOrEmpty(status))
                return true;

            return Enum.TryParse<Domain.Enums.BannerEventSpecial.EventStatus>(status, true, out _);
        }
    }
}
