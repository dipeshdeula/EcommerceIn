/*using FluentValidation;

namespace Application.Features.Authentication.Queries.Login
{
    public class LoginQueryValidator : AbstractValidator<LoginQuery>
    {
        public LoginQueryValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is Required.")
                .EmailAddress().WithMessage("Invalid Email Address.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is Required.")
                .MinimumLength(8).WithMessage("Password must be 8 characters long.");
        }
    }
}


*/