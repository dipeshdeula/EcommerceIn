using Application.Common.Helper;
using Application.Features.CompanyInfoFeat.Commands;
using Application.Interfaces.Repositories;
using FluentValidation;
using System.Text.RegularExpressions;

namespace Application.Features.CompanyInfoFeat.Validators
{
    public class UpdateCompanyInfoValidator : AbstractValidator<UpdateCompanyInfoCommand>
    {
        private readonly ICompanyInfoRepository _companyInfoRepository;
        public UpdateCompanyInfoValidator(ICompanyInfoRepository companyInfoRepository)
        {
            _companyInfoRepository = companyInfoRepository;

            RuleFor(x => x.Id)
               .NotEmpty().WithMessage("CompanyInfo ID is required.")
               .MustAsync(async (id, cancellation) => await _companyInfoRepository.AnyAsync(c => c.Id == id))
               .WithMessage("Company doesn't exist!");

            RuleFor(x => x.updateCompanyInfoDto.Name)
              //.MustAsync(async (name, cancellation) => !await _companyInfoRepository.AnyAsync(c => c.Name.ToLower() == name.ToLower()))
              //.WithMessage("Company name already exists.")
              .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.")
              .MinimumLength(3).WithMessage("Name must be at least 3 characters long.");

            RuleFor(x => x.updateCompanyInfoDto.Contact)
                  .NotEmpty().WithMessage("Contact is required")
                  .Length(10).WithMessage("Contact must be 10 characters long")
                  .Matches(@"^9[78]\d{8}$").WithMessage("Invalid contact number where it should start with 97 or 98")
                  .MustAsync(async (model, contact, cancellation) => await IsContactUniqueAsync(model.Id, contact))
                  .WithMessage("Contact number already exists");

            RuleFor(x => x.updateCompanyInfoDto.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .Must(email => !string.IsNullOrWhiteSpace(email?.Trim()))
                .WithMessage("Email cannot be just spaces")
                .Must(email => !email.Any(char.IsWhiteSpace))
                .WithMessage("Email cannot contain spaces")
                .Must(email => Regex.IsMatch(email.Trim(),
                    @"^(?=.{6,254}$)[A-Za-z]{3,}[A-Za-z0-9._%+-]*@([A-Za-z0-9-]+\.)+[A-Za-z]{2,}$"))
                    .WithMessage("Email must start with at least 3 letters, contain no spaces, and have a valid domain")
                .MustAsync(async (model, email, cancellation) => await IsEmailUniqueAsync(model.Id, email.Trim().ToLowerInvariant()))
                .WithMessage("Email already exists");

            RuleFor(x => x.updateCompanyInfoDto.RegistrationNumber)
                .MinimumLength(5).WithMessage("Registration number must be atleast 5 character long")               
                .When(x => x.updateCompanyInfoDto.RegistrationNumber is not null);

            RuleFor(x => x.updateCompanyInfoDto.RegisteredPanNumber)
                .MinimumLength(5).WithMessage("Registration Pan number must be atleast 5 character long")
                .When(x => x.updateCompanyInfoDto.RegisteredPanNumber is not null);
           

            RuleFor(x => x.updateCompanyInfoDto.Province)
                .Must(DataHelper.IsValidProvince!)
                .WithMessage("Invalid province name")
                .When(x => x.updateCompanyInfoDto.Province is not null);

            RuleFor(x => x.updateCompanyInfoDto.City)
                .Must(DataHelper.IsValidCity!)
                .WithMessage("Invalid city name")
                .When(x => x.updateCompanyInfoDto.City is not null);

            RuleFor(x => x.updateCompanyInfoDto.PostalCode)
                .Matches(@"^\d{5}$").WithMessage("Postal code must be a 5-digit number.")
                .When(x => x.updateCompanyInfoDto.PostalCode is not null);

            RuleFor(x => x.updateCompanyInfoDto.WebsiteUrl)
                .MinimumLength(10).WithMessage("Website url must be aleast 10 character long")
                .When(x => x.updateCompanyInfoDto.WebsiteUrl is not null);
        }

        private async Task<bool> IsContactUniqueAsync(int id, string contact)
        {
            return !await _companyInfoRepository.AnyAsync(x => x.Contact == contact && x.Id != id);
        }

        private async Task<bool> IsEmailUniqueAsync(int id, string email)
        {
            return !await _companyInfoRepository.AnyAsync(x => x.Email == email && x.Id != id);
        }


    }

}

