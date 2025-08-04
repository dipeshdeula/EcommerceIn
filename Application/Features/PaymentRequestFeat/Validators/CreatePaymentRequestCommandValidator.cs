using Application.Features.PaymentRequestFeat.Commands;
using FluentValidation;

namespace Application.Features.PaymentRequestFeat.Validators
{
    public class CreatePaymentRequestCommandValidator : AbstractValidator<CreatePaymentRequestCommand>
    {
        public CreatePaymentRequestCommandValidator()
        {
            RuleFor(x => x.addpaymentRequest)
                .NotNull()
                .WithMessage("Payment request data is required");

            RuleFor(x => x.addpaymentRequest.UserId)
                .GreaterThan(0)
                .WithMessage("Valid User ID is required");

            RuleFor(x => x.addpaymentRequest.OrderId)
                .GreaterThan(0)
                .WithMessage("Valid Order ID is required");

            RuleFor(x => x.addpaymentRequest.PaymentMethodId)
                .GreaterThan(0)
                .WithMessage("Valid Payment Method ID is required")
                .Must(BeValidPaymentMethod)
                .WithMessage("Payment method must be 1 (Esewa), 2 (Khalti), or 3 (COD)");

            RuleFor(x => x.addpaymentRequest.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters");
        }

        private bool BeValidPaymentMethod(int paymentMethodId)
        {
            return paymentMethodId >= 1 && paymentMethodId <= 5;
        }
    }
}
