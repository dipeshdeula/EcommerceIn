using Application.Common;
using Application.Enums;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Authentication.UploadImage.Commands
{
    public record UploadImageCommand(IFormFile File, int UserId) : IRequest<Result<User>>;

    public class UploadImageCommandHandler : IRequestHandler<UploadImageCommand, Result<User>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IFileServices _fileService;

        public UploadImageCommandHandler(IUserRepository userRepository, IFileServices fileService)
        {
            _userRepository = userRepository;
            _fileService = fileService;
        }

        public async Task<Result<User>> Handle(UploadImageCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.FindByIdAsync(request.UserId);
            if (user is null)
            {
                return Result<User>.Failure("User not found");
            }

            string fileUrl = null;
            if (request.File != null)
            {
                fileUrl = await _fileService.SaveFileAsync(request.File, FileType.UserImages);
            }

            user.ImageUrl = fileUrl;
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            return Result<User>.Success(user, "User image updated successfully");
        }
    }
}

