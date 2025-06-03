using Application.Interfaces.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Application.Features.StoreFeat.Commands
{
    public class CreateStoreCommandValidator : AbstractValidator<CreateStoreCommand>
    {
        private readonly IStoreRepository _storeRepository;
        public CreateStoreCommandValidator(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Store name is required.")
                .Matches("^[a-zA-Z ]*$").WithMessage("Store Name must contain only alphabets")
                .MinimumLength(3).WithMessage("Name must be at least 3 characters long");

            RuleFor(x => x.OwnerName)
                .NotEmpty().WithMessage("Owner name cannot be empty.")
                .Matches("^[a-zA-Z ]*$").WithMessage("Owner Name must contain only alphabets")
                .MinimumLength(3).WithMessage("Owner Name must be at least 3 characters long");

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
