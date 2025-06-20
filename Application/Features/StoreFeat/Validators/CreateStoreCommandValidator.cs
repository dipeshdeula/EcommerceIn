using Application.Features.StoreFeat.Commands;
using Application.Interfaces.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Application.Features.StoreFeat.Validators
{
    public class CreateStoreCommandValidator : AbstractValidator<CreateStoreCommand>
    {
        private readonly IStoreRepository _storeRepository;

        public CreateStoreCommandValidator(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Store name is required.")
                .Matches(@"^[a-zA-Z\s.]+$").WithMessage("Store name can only contain letters, spaces, and periods.")
                .MinimumLength(3).WithMessage("Store name must be at least 3 characters long");

            RuleFor(x => x.OwnerName)
                .NotEmpty().WithMessage("Owner name cannot be empty.")
                .Matches(@"^[a-zA-Z\s]+$").WithMessage("Owner name can only contain letters and spaces.")
                .MinimumLength(3).WithMessage("Owner name must be at least 3 characters long");

            RuleFor(x => x.File)
                .NotNull().WithMessage("Store image is required.")
                .Must(BeAValidImage).WithMessage("Only image files (jpg, jpeg, png) are allowed.")
                .Must(file => file.Length <= 5 * 1024 * 1024)
                .WithMessage("File size must be 5MB or less.");
        }

        private bool BeAValidImage(IFormFile file)
        {
            if (file == null) return false;

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
            return allowedTypes.Contains(file.ContentType.ToLower());
        }
    }
}
