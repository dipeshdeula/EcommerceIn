using Application.Common.Helper;
using Application.Dto.PromoCodeDTOs;
using Application.Features.PromoCodeFeat.Commands;
using FluentValidation;

namespace Application.Features.PromoCodeFeat.Validators
{
    public class UpdatePromoCodeCommandValidator : AbstractValidator<UpdatePromoCodeCommand>
    {
        public UpdatePromoCodeCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Promo code ID must be greater than 0");

            RuleFor(x => x.ModifiedByUserId)
                .GreaterThan(0)
                .WithMessage("Modified by user ID is required");

            // ✅ VALIDATE NEPAL DATE FORMATS USING TimeParsingHelper
            When(x => !string.IsNullOrEmpty(x.PromoCodeData.StartDateNepal), () =>
            {
                RuleFor(x => x.PromoCodeData.StartDateNepal)
                    .Must(BeValidDateTimeUsingTimeParsingHelper)
                    .WithMessage(x => GetDateTimeValidationMessage(x.PromoCodeData.StartDateNepal!, "start date"));

                RuleFor(x => x.PromoCodeData)
                    .Must(x => x.StartDateParsed.HasValue)
                    .WithMessage("Unable to parse start date. Please check the format.")
                    .When(x => !string.IsNullOrEmpty(x.PromoCodeData.StartDateNepal));
            });

            When(x => !string.IsNullOrEmpty(x.PromoCodeData.EndDateNepal), () =>
            {
                RuleFor(x => x.PromoCodeData.EndDateNepal)
                    .Must(BeValidDateTimeUsingTimeParsingHelper)
                    .WithMessage(x => GetDateTimeValidationMessage(x.PromoCodeData.EndDateNepal!, "end date"));

                RuleFor(x => x.PromoCodeData)
                    .Must(x => x.EndDateParsed.HasValue)
                    .WithMessage("Unable to parse end date. Please check the format.")
                    .When(x => !string.IsNullOrEmpty(x.PromoCodeData.EndDateNepal));
            });

            // ✅ VALIDATE DATE RANGE WITH PROPER NEPAL TIME COMPARISON
            RuleFor(x => x.PromoCodeData)
                .Must(HaveValidDateRange)
                .WithMessage("End date must be after start date (Nepal time)")
                .When(x => x.PromoCodeData.HasDateUpdates);

            // ✅ VALIDATE DISCOUNT VALUE
            When(x => x.PromoCodeData.DiscountValue.HasValue, () =>
            {
                RuleFor(x => x.PromoCodeData.DiscountValue!.Value)
                    .GreaterThan(0)
                    .LessThanOrEqualTo(100)
                    .WithMessage("Discount value must be between 0.01 and 100");
            });

            // ✅ VALIDATE MIN ORDER AMOUNT
            When(x => x.PromoCodeData.MinOrderAmount.HasValue, () =>
            {
                RuleFor(x => x.PromoCodeData.MinOrderAmount!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Minimum order amount cannot be negative");
            });

            // ✅ VALIDATE MAX DISCOUNT AMOUNT
            When(x => x.PromoCodeData.MaxDiscountAmount.HasValue, () =>
            {
                RuleFor(x => x.PromoCodeData.MaxDiscountAmount!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Maximum discount amount cannot be negative");
            });

            // ✅ VALIDATE USAGE LIMITS
            When(x => x.PromoCodeData.MaxTotalUsage.HasValue, () =>
            {
                RuleFor(x => x.PromoCodeData.MaxTotalUsage!.Value)
                    .GreaterThan(0)
                    .WithMessage("Max total usage must be greater than 0");
            });

            When(x => x.PromoCodeData.MaxUsagePerUser.HasValue, () =>
            {
                RuleFor(x => x.PromoCodeData.MaxUsagePerUser!.Value)
                    .GreaterThan(0)
                    .WithMessage("Max usage per user must be greater than 0");
            });
        }

        // ✅ USE TimeParsingHelper FOR VALIDATION
        private bool BeValidDateTimeUsingTimeParsingHelper(string? dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString)) return false;
            
            var result = TimeParsingHelper.ParseFlexibleDateTime(dateString);
            return result.Succeeded;
        }

        // ✅ GENERATE HELPFUL ERROR MESSAGES USING TimeParsingHelper
        private string GetDateTimeValidationMessage(string input, string fieldName)
        {
            var result = TimeParsingHelper.ParseFlexibleDateTime(input);
            if (result.Succeeded) return $"{fieldName} is valid";
            
            return $"Invalid {fieldName} format: '{input}'. {TimeParsingHelper.GetParsingErrorMessage(input)}";
        }

        private bool HaveValidDateRange(UpdatePromoCodeDTO dto)
        {
            return dto.IsValidDateRange;
        }
    }
}