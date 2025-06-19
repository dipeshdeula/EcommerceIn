using Application.Common;
using Application.Common.Helper;
using Application.Dto.AuthDTOs;
using Application.Features.Authentication.Commands;
using Application.Interfaces.Repositories;
using Application.Utilities;
using MediatR;

namespace Application.Features.Authentication.Otp.Commands
{
    public record VerifyOtpCommand(string Email, string Otp) : IRequest<Result<RegisterCommand>>;

    public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, Result<RegisterCommand>>
    {
        private readonly IUserRepository _userRepository;
        private readonly OtpService _otpService;

        public VerifyOtpCommandHandler(IUserRepository userRepository, OtpService otpService)
        {
            _userRepository = userRepository;
            _otpService = otpService;
        }

        public async Task<Result<RegisterCommand>> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
        {
            // ✅ Step 1: Validate OTP
            if (!_otpService.ValidateOtp(request.Email, request.Otp))
            {
                return Result<RegisterCommand>.Failure("Invalid or expired OTP.");
            }

            // ✅ Step 2: Get user and password from OTPService cache/memory
            var (user, rawPassword) = _otpService.GetUserInfo(request.Email);
            if (user == null || string.IsNullOrEmpty(rawPassword))
            {
                return Result<RegisterCommand>.Failure("User information not found.");
            }

            // ✅ Step 3: Hash password and save
            user.Password = PasswordHelper.HashPassword(rawPassword);
            user.IsDeleted = false;

            try
            {
                await _userRepository.AddAsync(user, cancellationToken);
                await _userRepository.SaveChangesAsync(cancellationToken);

                var registerDto = new RegisterUserDTO
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Password = rawPassword,
                    Contact = user.Contact,

                };

                var registerCommand = new RegisterCommand(registerDto);
                return Result<RegisterCommand>.Success(registerCommand, "OTP verified successfully. Account has been created.");
            }
            catch (Exception ex)
            {
                return Result<RegisterCommand>.Failure("User registration failed.", new[] { ex.Message });
            }
        }
    }
}
