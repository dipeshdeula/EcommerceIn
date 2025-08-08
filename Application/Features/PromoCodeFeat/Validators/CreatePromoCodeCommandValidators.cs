using Application.Dto.PromoCodeDTOs;
using Application.Features.PromoCodeFeat.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.Features.PromoCodeFeat.Validators
{
    public class CreatePromoCodeCommandValidator : AbstractValidator<AddPromoCodeDTO>
    {
        public CreatePromoCodeCommandValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Promo code is required")
                .Length(3, 50).WithMessage("Promo code must be between 3 and 50 characters")
                .Matches("^[A-Z0-9]+$").WithMessage("Promo code can only contain uppercase letters and numbers");
                
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Promo code name is required")
                .Length(2, 200).WithMessage("Name must be between 2 and 200 characters");
                
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");
                
            RuleFor(x => x.DiscountValue)
                .GreaterThan(0).WithMessage("Discount value must be greater than 0");
                
            // Percentage validation
            When(x => x.Type == PromoCodeType.Percentage, () => {
                RuleFor(x => x.DiscountValue)
                    .LessThanOrEqualTo(100).WithMessage("Percentage discount cannot exceed 100%");
            });
            
            // Fixed amount validation
            When(x => x.Type == PromoCodeType.FixedAmount, () => {
                RuleFor(x => x.DiscountValue)
                    .LessThanOrEqualTo(10000).WithMessage("Fixed discount cannot exceed Rs.10,000");
            });
            
            RuleFor(x => x.MaxDiscountAmount)
                .GreaterThan(0).When(x => x.MaxDiscountAmount.HasValue)
                .WithMessage("Maximum discount amount must be greater than 0");
                
            RuleFor(x => x.MinOrderAmount)
                .GreaterThan(0).When(x => x.MinOrderAmount.HasValue)
                .WithMessage("Minimum order amount must be greater than 0");
                
            RuleFor(x => x.MaxTotalUsage)
                .GreaterThan(0).When(x => x.MaxTotalUsage.HasValue)
                .WithMessage("Maximum total usage must be greater than 0");
                
            RuleFor(x => x.MaxUsagePerUser)
                .GreaterThan(0).When(x => x.MaxUsagePerUser.HasValue)
                .WithMessage("Maximum usage per user must be greater than 0");
                
            RuleFor(x => x.StartDateNepal)
                .LessThan(x => x.EndDateNepal)
                .WithMessage("Start date must be before end date");
                
            RuleFor(x => x.EndDateNepal)
                .GreaterThan(x=>x.StartDateNepal)
                .WithMessage("End date must be in the future");
                
            
        }
    }
    
    /*public class ApplyPromoCodeCommandValidator : AbstractValidator<ApplyPromoCodeCommand>
    {
        public ApplyPromoCodeCommandValidator()
        {
            RuleFor(x => x.Request.Code)
                .NotEmpty().WithMessage("Promo code is required");
                
            RuleFor(x => x.Request.UserId)
                .GreaterThan(0).WithMessage("User ID is required");
                
            RuleFor(x => x.Request.OrderTotal)
                .GreaterThan(0).WithMessage("Order total must be greater than 0");
                
            RuleFor(x => x.Request.ShippingCost)
                .GreaterThanOrEqualTo(0).WithMessage("Shipping cost cannot be negative");
        }
    }*/
}