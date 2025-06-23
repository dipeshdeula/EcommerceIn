using Application.Features.PaymentMethodFeat.Commands;
using Application.Interfaces.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.PaymentMethodFeat.Validators
{
    public class UpdatePaymentMethodCommandValidator : AbstractValidator<UpdatePaymentMethodCommand>
    {
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        public UpdatePaymentMethodCommandValidator(IPaymentMethodRepository paymentMethodRepository)
        {
            _paymentMethodRepository = paymentMethodRepository;

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Payment Method Id is required.")
                .MustAsync(async (id, cancellationToken) => await _paymentMethodRepository.AnyAsync(p => p.Id == id))
                .WithMessage("Payment method doesn't exist");

            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Name must be 100 characters or fewer.")               
                .When(x => x.Name is not null);

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid payment method type.")
                .When(x => x.Type is not null);

            RuleFor(x => x.File)
                .Must(BeAnImage).WithMessage("Only image files (jpg, jpeg, png) are allowed.")
                .Must(f => f.Length <= 5 * 1024 * 1024).WithMessage("File size must not exceed 5MB.")
                .When(x => x.File is not null);
        }
        private bool BeAnImage(IFormFile file)
        {
            if (file == null) return false;

            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
            return allowedMimeTypes.Contains(file.ContentType.ToLower());
        }
    }
}
