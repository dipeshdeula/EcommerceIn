using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IOtpService
    {
        string GenerateOtp(string email);
        bool ValidateOtp(string email, string otp);
        void StoreUserInfo(string email, User user, string password);
        (User user, string password) GetUserInfo(string email);

        void RemoveOtp(string email);
        void RemoveUserInfo(string email);
        TimeSpan? GetOtpRemainingTime(string email);

    }
} 