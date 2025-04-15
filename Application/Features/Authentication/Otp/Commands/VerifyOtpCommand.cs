using Application.Common;
using Application.Common.Helper;
using Application.Interfaces.Repositories;
using Application.Utilities;
using MediatR;

namespace Application.Features.Authentication.Otp.Commands
{
    public record VerifyOtpCommand(string Email, string Otp) : IRequest<Result>;

    public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, Result>
    {
        private readonly IUserRepository _userRepository;
        private readonly OtpService _otpService;

        public VerifyOtpCommandHandler(IUserRepository userRepository, OtpService otpService)
        {
            _userRepository = userRepository;
            _otpService = otpService;
        }

        public async Task<Result> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
        {
            // ✅ Step 1: Validate OTP
            if (!_otpService.ValidateOtp(request.Email, request.Otp))
            {
                return Result.Failure("Invalid or expired OTP.");
            }

            // ✅ Step 2: Get user and password from OTPService cache/memory
            var (user, rawPassword) = _otpService.GetUserInfo(request.Email);
            if (user == null || string.IsNullOrEmpty(rawPassword))
            {
                return Result.Failure("User information not found.");
            }

            // ✅ Step 3: Hash password and save
            user.Password = PasswordHelper.HashPassword(rawPassword);
            user.IsDeleted = false;

            try
            {
                await _userRepository.AddAsync(user, cancellationToken);
                await _userRepository.SaveChangesAsync(cancellationToken);

                return Result.Success("OTP verified successfully. Account has been created.");
            }
            catch (Exception ex)
            {
                return Result.Failure("User registration failed.", new[] { ex.Message });
            }
        }
    }
}
