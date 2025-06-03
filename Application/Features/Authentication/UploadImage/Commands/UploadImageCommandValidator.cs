using Application.Interfaces.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Authentication.UploadImage.Commands
{
    public class UploadImageCommandValidator : AbstractValidator<UploadImageCommand>
    {
        private readonly IUserRepository _userRepository;

        public UploadImageCommandValidator(IUserRepository userRepository)
        {
            _userRepository = userRepository;

            RuleFor(x => x.File)
                .NotNull().WithMessage("File is required.")
                .Must(BeAnImage).WithMessage("Only image files (jpg, png, jpeg) are allowed.");

            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("UserId must be greater than 0.")
                .MustAsync(async (userId, cancellation) =>
                    await _userRepository.AnyAsync(u => u.Id == userId))
                .WithMessage("User does not exist.");
        }

        private bool BeAnImage(IFormFile file)
        {
            if (file == null)
                return false;

            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
            return allowedMimeTypes.Contains(file.ContentType.ToLower());
        }
    }
}
