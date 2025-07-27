using Application.Common;
using Application.Common.Helper;
using Application.Dto.AuthDTOs;
using Application.Dto.UserDTOs;
using Application.Extension;
using Application.Features.Authentication.Commands.UserInfo.Commands;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utilities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Authentication.Otp.Commands
{
    public record VerifyForgotPasswordCommand 
        (string Email, string Otp) : IRequest<Result<ForgotPasswordDTO>>;

    public class VerifyOtpForgotPasswordDTOHandler : IRequestHandler<VerifyForgotPasswordCommand, Result<ForgotPasswordDTO>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IOtpService _otpService;
        public VerifyOtpForgotPasswordDTOHandler(
            IUserRepository userRepository,
            IOtpService otpService)
        {
            _userRepository = userRepository;
            _otpService = otpService;
        }

        public async Task<Result<ForgotPasswordDTO>> Handle(VerifyForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (!_otpService.ValidateOtp(request.Email, request.Otp))
                {
                    return Result<ForgotPasswordDTO>.Failure("Invalid or expired OTP.");
                }

                var (user, rawPassword) = _otpService.GetUserInfo(request.Email);
                if (user == null || string.IsNullOrEmpty(rawPassword))
                {
                    return Result<ForgotPasswordDTO>.Failure("User not found");
                }

                user.Password = PasswordHelper.HashPassword(rawPassword);

                await _userRepository.UpdateAsync(user, cancellationToken);
                await _userRepository.SaveChangesAsync(cancellationToken);

                var dto = new ForgotPasswordDTO
                {
                    Email = user.Email,
                    NewPassword = "" 
                };

                return Result<ForgotPasswordDTO>.Success(dto, "OTP verified successfully. Password has been changed.");
            }
            catch (Exception ex)
            {
                return Result<ForgotPasswordDTO>.Failure("Reset Password failed.", new[] { ex.Message });
            }
        }
    }
   
}
