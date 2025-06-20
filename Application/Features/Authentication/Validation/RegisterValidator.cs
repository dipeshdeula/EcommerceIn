using Application.Features.Authentication.Commands;
using Application.Interfaces.Repositories;
using FluentValidation;
using System.Text.RegularExpressions;
namespace Application.Features.Authentication.Validation
{
    public class RegisterValidator : AbstractValidator<RegisterCommand>
    {
        private readonly IUserRepository _userRepository;

        public RegisterValidator(IUserRepository userRepository)
        {
            _userRepository = userRepository;

            RuleFor(x => x.regUserDto.Name)
                .NotEmpty().WithMessage("Name is required")
                .Matches(@"^[a-zA-Z]+(\s[a-zA-Z]+)*$").WithMessage("Name must begin with letters and can contain spaces between words")
                .MinimumLength(3).WithMessage("Name must be at least 3 characters long");

            RuleFor(x => x.regUserDto.Contact)
                .NotEmpty().WithMessage("Contact is required")
                .Length(10).WithMessage("Contact must be 10 characters long")
                .Matches(@"^9[78]\d{8}$").WithMessage("Invalid contact number where it should start with 97 or 98")
                .MustAsync(async (model, contact, cancellation) => await IsContactUniqueAsync(model.regUserDto.Id, contact))
                .WithMessage("Contact number already exists");

            RuleFor(x => x.regUserDto.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .Must(email => !string.IsNullOrWhiteSpace(email?.Trim()))
                .WithMessage("Email cannot be just spaces")
                .Must(email => !email.Any(char.IsWhiteSpace))
                .WithMessage("Email cannot contain spaces")

                .Must(email => Regex.IsMatch(email.Trim(),
                    @"^(?=.{6,254}$)[A-Za-z]{3,}[A-Za-z0-9._%+-]*@([A-Za-z0-9-]+\.)+[A-Za-z]{2,}$"))
                    .WithMessage("Email must start with at least 3 letters, contain no spaces, and have a valid domain")
                .MustAsync(async (model, email, cancellation) => await IsEmailUniqueAsync(model.regUserDto.Id, email.Trim().ToLowerInvariant()))
                .WithMessage("Email already exists");

            RuleFor(x => x.regUserDto.Password)
                .NotEmpty().WithMessage("Password is Required")
                .MinimumLength(8).WithMessage("Password must be 8 characters long.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");
        }

        private async Task<bool> IsContactUniqueAsync(int id, string contact)
        {
            return !await _userRepository.AnyAsync(x => x.Contact == contact && x.Id != id);
        }

        private async Task<bool> IsEmailUniqueAsync(int id, string email)
        {
            return !await _userRepository.AnyAsync(x => x.Email == email && x.Id != id);
        }
    }
}


