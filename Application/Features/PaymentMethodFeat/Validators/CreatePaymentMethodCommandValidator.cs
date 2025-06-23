using Application.Features.PaymentMethodFeat.Commands;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.PaymentMethodFeat.Validators
{
    public class CreatePaymentMethodCommandValidator : AbstractValidator<CreatePaymentMethodCommand>
    {
        public CreatePaymentMethodCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must be 100 characters or fewer.");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid payment method type.");

            RuleFor(x => x.File)
                .NotNull().WithMessage("Logo file is required.")
                .Must(BeAnImage).WithMessage("Only image files (jpg, jpeg, png) are allowed.")
                .Must(f => f.Length <= 5 * 1024 * 1024).WithMessage("File size must not exceed 5MB.");
        }

        private bool BeAnImage(IFormFile file)
        {
            if (file == null) return false;

            var allowedMimeTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/jpg",
                "image/svg+xml" // Correct MIME type for SVG
            };

            return allowedMimeTypes.Contains(file.ContentType.ToLower());
        }

    }
}
