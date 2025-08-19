using Application.Common.Helper;
using Application.Dto.PromoCodeDTOs;
using Application.Features.PromoCodeFeat.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.Features.PromoCodeFeat.Validators
{
    public class CreatePromoCodeCommandValidator : AbstractValidator<CreatePromoCodeCommand>
    {
        public CreatePromoCodeCommandValidator()
        {
            //  VALIDATE COMMAND STRUCTURE
            RuleFor(x => x.CreatedByUserId)
                .GreaterThan(0)
                .WithMessage("Created by user ID is required");

            RuleFor(x => x.PromoCodeDTO)
                .NotNull()
                .WithMessage("Promo code data is required");

            //  VALIDATE PROMO CODE PROPERTIES
            When(x => x.PromoCodeDTO != null, () => {
                
                //  CODE VALIDATION
                RuleFor(x => x.PromoCodeDTO.Code)
                    .NotEmpty().WithMessage("Promo code is required")
                    .Length(3, 50).WithMessage("Promo code must be between 3 and 50 characters")
                    .Matches("^[A-Z0-9]+$").WithMessage("Promo code can only contain uppercase letters and numbers");
                    
                //  NAME VALIDATION
                RuleFor(x => x.PromoCodeDTO.Name)
                    .NotEmpty().WithMessage("Promo code name is required")
                    .Length(2, 200).WithMessage("Name must be between 2 and 200 characters");
                    
                //  DESCRIPTION VALIDATION
                RuleFor(x => x.PromoCodeDTO.Description)
                    .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
                    .When(x => !string.IsNullOrEmpty(x.PromoCodeDTO.Description));
                    
                //  DISCOUNT VALUE VALIDATION
                RuleFor(x => x.PromoCodeDTO.DiscountValue)
                    .GreaterThan(0).WithMessage("Discount value must be greater than 0")
                    .LessThanOrEqualTo(100).WithMessage("Discount percentage cannot exceed 100%");
                    
                //  MAX DISCOUNT AMOUNT VALIDATION (fixed - remove .HasValue since it's not nullable)
                RuleFor(x => x.PromoCodeDTO.MaxDiscountAmount)
                    .GreaterThanOrEqualTo(0).WithMessage("Maximum discount amount cannot be negative");
                    
                //  MIN ORDER AMOUNT VALIDATION (fixed - remove .HasValue since it's not nullable)
                RuleFor(x => x.PromoCodeDTO.MinOrderAmount)
                    .GreaterThanOrEqualTo(0).WithMessage("Minimum order amount cannot be negative");
                    
                //  MAX TOTAL USAGE VALIDATION (fixed - remove .HasValue since it's not nullable)
                RuleFor(x => x.PromoCodeDTO.MaxTotalUsage)
                    .GreaterThan(0).WithMessage("Maximum total usage must be greater than 0");
                    
                //  MAX USAGE PER USER VALIDATION (fixed - remove .HasValue since it's not nullable)
                RuleFor(x => x.PromoCodeDTO.MaxUsagePerUser)
                    .GreaterThan(0).WithMessage("Maximum usage per user must be greater than 0");
                    
                //  NEPAL DATE FORMAT VALIDATION USING TimeParsingHelper
                RuleFor(x => x.PromoCodeDTO.StartDateNepal)
                    .NotEmpty().WithMessage("Start date is required")
                    .Must(BeValidDateTimeUsingTimeParsingHelper)
                    .WithMessage(x => GetDateTimeValidationMessage(x.PromoCodeDTO.StartDateNepal, "start date"));

                RuleFor(x => x.PromoCodeDTO.EndDateNepal)
                    .NotEmpty().WithMessage("End date is required")
                    .Must(BeValidDateTimeUsingTimeParsingHelper)
                    .WithMessage(x => GetDateTimeValidationMessage(x.PromoCodeDTO.EndDateNepal, "end date"));
                    
                //  DATE RANGE VALIDATION USING PARSED DATES
                RuleFor(x => x.PromoCodeDTO)
                    .Must(HaveValidDateRange)
                    .WithMessage("End date must be after start date")
                    .When(x => x.PromoCodeDTO.HasValidDateFormats);

                //  FUTURE DATE VALIDATION
                RuleFor(x => x.PromoCodeDTO)
                    .Must(HaveValidStartDate)
                    .WithMessage("Start date cannot be more than 1 hour in the past")
                    .When(x => x.PromoCodeDTO.HasValidDateFormats);

                //  DURATION VALIDATION
                RuleFor(x => x.PromoCodeDTO)
                    .Must(HaveValidDuration)
                    .WithMessage("Promo code duration cannot exceed 365 days")
                    .When(x => x.PromoCodeDTO.HasValidDateFormats);

                
            });
        }

        //  USE TimeParsingHelper FOR DATE VALIDATION
        private bool BeValidDateTimeUsingTimeParsingHelper(string? dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString)) return false;
            
            var result = TimeParsingHelper.ParseFlexibleDateTime(dateString);
            return result.Succeeded;
        }

        //  GENERATE HELPFUL ERROR MESSAGES USING TimeParsingHelper
        private string GetDateTimeValidationMessage(string input, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(input)) 
                return $"{fieldName} is required";

            var result = TimeParsingHelper.ParseFlexibleDateTime(input);
            if (result.Succeeded) 
                return $"{fieldName} is valid";
            
            return $"Invalid {fieldName} format: '{input}'. {result.Message}. " +
                   $"Examples: {string.Join(", ", TimeParsingHelper.GetSupportedFormats().Take(3))}";
        }

        //  VALIDATE DATE RANGE
        private bool HaveValidDateRange(AddPromoCodeDTO dto)
        {
            return dto.IsValidDateRange;
        }

        //  VALIDATE START DATE IS NOT TOO FAR IN PAST
        private bool HaveValidStartDate(AddPromoCodeDTO dto)
        {
            if (!dto.StartDateParsed.HasValue) return false;

            var now = DateTime.Now; // Current Nepal time (approximate)
            var timeDiff = now - dto.StartDateParsed.Value;
            
            return timeDiff.TotalHours <= 1; // Allow up to 1 hour in the past
        }

        //  VALIDATE DURATION IS REASONABLE
        private bool HaveValidDuration(AddPromoCodeDTO dto)
        {
            if (!dto.StartDateParsed.HasValue || !dto.EndDateParsed.HasValue) return false;

            var duration = dto.EndDateParsed.Value - dto.StartDateParsed.Value;
            return duration.TotalDays <= 365; // Max 1 year duration
        }
    }

    //  SEPARATE VALIDATOR FOR DTO ONLY (if needed elsewhere)
    public class AddPromoCodeDTOValidator : AbstractValidator<AddPromoCodeDTO>
    {
        public AddPromoCodeDTOValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Promo code is required")
                .Length(3, 50).WithMessage("Promo code must be between 3 and 50 characters")
                .Matches("^[A-Z0-9]+$").WithMessage("Promo code can only contain uppercase letters and numbers");
                
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Promo code name is required")
                .Length(2, 200).WithMessage("Name must be between 2 and 200 characters");
                
            RuleFor(x => x.DiscountValue)
                .GreaterThan(0).WithMessage("Discount value must be greater than 0")
                .LessThanOrEqualTo(100).WithMessage("Discount percentage cannot exceed 100%");
                
            RuleFor(x => x.MaxDiscountAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Maximum discount amount cannot be negative");
                
            RuleFor(x => x.MinOrderAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Minimum order amount cannot be negative");
                
            RuleFor(x => x.MaxTotalUsage)
                .GreaterThan(0).WithMessage("Maximum total usage must be greater than 0");
                
            RuleFor(x => x.MaxUsagePerUser)
                .GreaterThan(0).WithMessage("Maximum usage per user must be greater than 0");

            //  DATE VALIDATION USING TimeParsingHelper
            RuleFor(x => x.StartDateNepal)
                .NotEmpty().WithMessage("Start date is required")
                .Must(BeValidDateTime)
                .WithMessage("Invalid start date format. Examples: '08/12/2025 5:13 PM', '2025-08-12 17:13'");

            RuleFor(x => x.EndDateNepal)
                .NotEmpty().WithMessage("End date is required")
                .Must(BeValidDateTime)
                .WithMessage("Invalid end date format. Examples: '08/14/2025 5:03 PM', '2025-08-14 17:03'");

            RuleFor(x => x)
                .Must(x => x.IsValidDateRange)
                .WithMessage("End date must be after start date")
                .When(x => x.HasValidDateFormats);
        }

        private bool BeValidDateTime(string? dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString)) return false;
            var result = TimeParsingHelper.ParseFlexibleDateTime(dateString);
            return result.Succeeded;
        }
    }
}