using Application.Interfaces.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Application.Features.StoreFeat.Commands
{
    public class UpdateStoreCommandValidator : AbstractValidator<UpdateStoreCommand>
    {
        private readonly IStoreRepository _storeRepository;
        public UpdateStoreCommandValidator(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Store Id must be greater than 0.");
               

            When(x => x.Name is not null, () =>
            {
                RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("Store name cannot be empty.")
                    .Matches("^[a-zA-Z ]*$").WithMessage("Store Name must contain only alphabets")
                    .MinimumLength(3).WithMessage("Name must be at least 3 characters long");
            });

            When(x => x.OwnerName is not null, () =>
            {
                RuleFor(x => x.OwnerName)
                    .NotEmpty().WithMessage("Owner name cannot be empty.")
                    .Matches("^[a-zA-Z ]*$").WithMessage("Owner Name must contain only alphabets")
                    .MinimumLength(3).WithMessage("Owner Name must be at least 3 characters long");
            });

            When(x => x.FIle is not null, () =>
            {
                RuleFor(x => x.FIle)
                    .NotNull().WithMessage("Store image is required.")
                    .Must(BeAValidImage).WithMessage("Only image files (jpg, jpeg, png) are allowed.")
                    .Must(file => file.Length <= 5 * 1024 * 1024)
                    .WithMessage("File size must be 5MB or less.");
            });
        }

        private bool BeAValidImage(IFormFile file)
        {
            if (file == null) return false;

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
            return allowedTypes.Contains(file.ContentType.ToLower());
        }
    }
}
