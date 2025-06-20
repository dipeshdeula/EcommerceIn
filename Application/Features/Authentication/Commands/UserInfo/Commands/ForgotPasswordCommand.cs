using Application.Common;
using Application.Common.Helper;
using Application.Dto.AuthDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utilities;
using Domain.Entities;
using MediatR;

namespace Application.Features.Authentication.Commands.UserInfo.Commands
{
    public record ForgotPasswordCommand(
        ForgotPasswordDTO forgotPasswordDto
        ) : IRequest<Result<string>>;
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly OtpService _otpService;
        private readonly IEmailService _emailService;

        public ForgotPasswordCommandHandler(
            IUserRepository userRepository,
            OtpService otpService,
            IEmailService emailService
            )
        {
            _userRepository = userRepository;
            _otpService = otpService;
            _emailService = emailService;

        }

        public async Task<Result<string>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userRepository.FirstOrDefaultAsync(x => x.Email == request.forgotPasswordDto.Email);
                if (user == null)
                {
                    return Result<string>.Failure("Email not found");
                }

                var otp = _otpService.GenerateOtp(request.forgotPasswordDto.Email);
                await _emailService.SendEmailAsync(user.Email, "Forgot Password", $"Your OTP code is : {otp}");

                // Store hashed password for security
                var hashedPassword = PasswordHelper.HashPassword(request.forgotPasswordDto.NewPassword);

                var changePass = new User
                {
                    Name = user.Name,
                    Email = user.Email,
                    Password = hashedPassword,
                    Contact = user.Contact,
                    Id = user.Id 
                };
                _otpService.StoreUserInfo(request.forgotPasswordDto.Email, changePass, request.forgotPasswordDto.NewPassword);

                return Result<string>.Success("OTP has been sent to your email. Please verify your email with the OTP sent to you.");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Error: {ex.Message}");
            }
        }

    }
}
