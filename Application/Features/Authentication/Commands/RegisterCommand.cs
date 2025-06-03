using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utilities;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Application.Features.Authentication.Commands
{
    public record RegisterCommand(
        int Id,
        string Name,
        string Email,
        string Password,
        string Contact
        ) : IRequest<Result<UserDTO>>;

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<UserDTO>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IFileServices _fileService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly OtpService _otpService;

        public RegisterCommandHandler(IUserRepository userRepository, IFileServices fileService, IEmailService emailService, IConfiguration configuration, OtpService otpService)
        {
            _userRepository = userRepository;
            _fileService = fileService;
            _emailService = emailService;
            _configuration = configuration;
            _otpService = otpService;
        }

        public async Task<Result<UserDTO>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var userExist = await _userRepository.GetByEmailAsync(request.Email);
                if (userExist != null)
                {
                    return Result<UserDTO>.Failure("User already exists");
                }

                var otp = _otpService.GenerateOtp(request.Email);
                await _emailService.SendEmailAsync(request.Email, "Account Verification", $"Your OTP code is: {otp}");

                var user = new User
                {
                    Name = request.Name,
                    Email = request.Email,
                    Password = request.Password,
                    Contact = request.Contact,
                    CreatedAt = DateTime.UtcNow,
                };

                
                _otpService.StoreUserInfo(request.Email, user, request.Password);

                return Result<UserDTO>.Success(user.ToDTO(), "OTP has been sent to your email. Please verify your email with the OTP sent to you");

               
            }
            catch (Exception ex)
            {
                return Result<UserDTO>.Failure($"Invalid OTP: {ex.Message}");
            }
        }
    }
}



