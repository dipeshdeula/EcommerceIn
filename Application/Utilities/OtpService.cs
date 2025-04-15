using Domain.Entities;
using Domain.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Utilities
{
    public class OtpService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IConfiguration _configuration;
        private readonly OtpSettings _otpSettings;

        public OtpService(IMemoryCache memoryCache, IConfiguration configuration, OtpSettings otpSettings)
        {
            _memoryCache = memoryCache;
            _configuration = configuration;
            _otpSettings = otpSettings;

        }

        public string GenerateOtp(string email)
        { 
            var otp = new Random().Next(100000, 999999).ToString();
            // var expirationTime = DateTime.UtcNow.AddMinutes(_otpSettings.ExpirationMinutes);
            var expirationMinutes = 5; // Set expiration time to 5 minutes
            _memoryCache.Set(email,otp,TimeSpan.FromMinutes(expirationMinutes));

            return otp;
        }

        public bool ValidateOtp(string email, string otp)
        {
            if (_memoryCache.TryGetValue(email, out string storedOtp))
            {
                return storedOtp == otp;
            }
            return false;
        }

        public void StoreUserInfo(string email, User user, string password)
        {
            var expirationMinutes = 5; // Hardcoded 5 minutes expiration time
            _memoryCache.Set(email + "_user", user, TimeSpan.FromMinutes(expirationMinutes));
            _memoryCache.Set(email + "_password", password, TimeSpan.FromMinutes(expirationMinutes));
        }

        public (User user, string password) GetUserInfo(string email)
        {
            _memoryCache.TryGetValue(email + "_user", out User user);
            _memoryCache.TryGetValue(email + "_password", out string password);
            return (user, password);
        }
    }
}
