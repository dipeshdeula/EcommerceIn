using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utilities;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Authentication.Commands
{
    public record RegisterCommand(
        int Id,
        string Name,
        string Email,
        string Password,
        string Contact
        ) : IRequest<IResult>;

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, IResult>
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

        public async Task<IResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var userExist = await _userRepository.GetByEmailAsync(request.Email);
                if (userExist != null)
                {
                    return Results.BadRequest("User already exists");
                }

                var otp = _otpService.GenerateOtp(request.Email);
                await _emailService.SendEmailAsync(request.Email, "Account Verification", $"Your OTP code is {otp}");

                var user = new User
                {
                    Name = request.Name,
                    Email = request.Email,
                    Password = request.Password,
                    Contact = request.Contact,
                    CreatedAt = DateTime.UtcNow,
                };

                //await _userRepository.AddAsync(user, cancellationToken);
                _otpService.StoreUserInfo(request.Email, user, request.Password);

                return Results.Ok(new
                {
                    Message = "OTP has been sent to your email. Please verify your email with the OTP sent to you"
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new
                {
                    Message = ex.Message,
                    StatusCode = StatusCodes.Status500InternalServerError
                });
            }
        }
    }
}



