using Application.Dto.BannerEventSpecialDTOs;
using Domain.Enums.BannerEventSpecial;
using FluentValidation;

namespace Application.Features.BannerSpecialEvent.Commands
{
    public class EventRuleDtoValidator : AbstractValidator<AddEventRuleDTO>
    {
        public EventRuleDtoValidator()
        {
            RuleFor(x => x.TargetValue)
                .NotEmpty().WithMessage("Rule target value is required.")
                .MaximumLength(500).WithMessage("Target value cannot exceed 500 characters.");

            RuleFor(x => x.DiscountValue)
                .GreaterThan(0).WithMessage("Rule discount value must be greater than 0.");

            RuleFor(x => x.DiscountValue)
                .LessThanOrEqualTo(100)
                .When(x => x.DiscountType == PromotionType.Percentage)
                .WithMessage("Percentage discount cannot exceed 100%");

            RuleFor(x => x.MaxDiscount)
                .GreaterThan(0)
                .When(x => x.MaxDiscount.HasValue)
                .WithMessage("Maximum discount must be greater than 0.");

            RuleFor(x => x.MinOrderValue)
                .GreaterThan(0)
                .When(x => x.MinOrderValue.HasValue)
                .WithMessage("Minimum order value must be greater than 0.");

            RuleFor(x => x.Priority)
                .GreaterThan(0).WithMessage("Rule priority must be greater than 0.");

            RuleFor(x => x.Conditions)
                .MaximumLength(1000).WithMessage("Conditions cannot exceed 1000 characters.");
        }
    }
}
