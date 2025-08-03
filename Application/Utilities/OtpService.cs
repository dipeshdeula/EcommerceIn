using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;

namespace Application.Utilities
{
    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IConfiguration _configuration;
        private readonly OtpSettings _otpSettings;
        private readonly ILogger<OtpService> _logger;

        public OtpService(
            IMemoryCache memoryCache, 
            IConfiguration configuration, 
            OtpSettings otpSettings,
            ILogger<OtpService> logger)
        {
            _memoryCache = memoryCache;
            _configuration = configuration;
            _otpSettings = otpSettings;
            _logger = logger;
        }

        public string GenerateOtp(string email)
        {
            try
            {
                // ✅ ENHANCED: Use cryptographically secure random
                using var rng = RandomNumberGenerator.Create();
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var otp = (Math.Abs(BitConverter.ToInt32(bytes, 0)) % 900000 + 100000).ToString();

                var expirationMinutes = 5;

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes),
                    Size = 1, // Small size for OTP string
                    Priority = CacheItemPriority.High,
                    SlidingExpiration = null // No sliding expiration for OTPs
                };

                // ✅ Remove old OTP if exists
                _memoryCache.Remove(email);
                
                _memoryCache.Set(email, otp, cacheOptions);
                
                _logger.LogInformation("OTP generated for email: {Email}, expires in {Minutes} minutes", 
                    email, expirationMinutes);

                return otp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP for email: {Email}", email);
                throw;
            }
        }

        public bool ValidateOtp(string email, string otp)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
                {
                    _logger.LogWarning("Invalid OTP validation attempt: empty email or OTP");
                    return false;
                }

                if (_memoryCache.TryGetValue(email, out string storedOtp))
                {
                    var isValid = storedOtp == otp;
                    
                    if (isValid)
                    {
                        // ✅ Remove OTP after successful validation (one-time use)
                        _memoryCache.Remove(email);
                        _logger.LogInformation("OTP validated successfully for email: {Email}", email);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid OTP attempt for email: {Email}", email);
                    }
                    
                    return isValid;
                }

                _logger.LogWarning("OTP not found or expired for email: {Email}", email);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating OTP for email: {Email}", email);
                return false;
            }
        }

        public void StoreUserInfo(string email, User user, string password)
        {
            try
            {
                var expirationMinutes =  5;

                var userCacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes),
                    Size = 10, // Larger size for User object
                    Priority = CacheItemPriority.High
                };

                var passwordCacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes),
                    Size = 2, // Small size for password string
                    Priority = CacheItemPriority.High
                };

                // ✅ Clean up existing entries
                RemoveUserInfo(email);

                _memoryCache.Set(email + "_user", user, userCacheOptions);
                _memoryCache.Set(email + "_password", password, passwordCacheOptions);

                _logger.LogInformation("User info stored in cache for email: {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing user info for email: {Email}", email);
                throw;
            }
        }

        public (User user, string password) GetUserInfo(string email)
        {
            try
            {
                _memoryCache.TryGetValue(email + "_user", out User user);
                _memoryCache.TryGetValue(email + "_password", out string password);

                if (user != null && !string.IsNullOrEmpty(password))
                {
                    _logger.LogInformation("User info retrieved from cache for email: {Email}", email);
                }
                else
                {
                    _logger.LogWarning("User info not found or expired for email: {Email}", email);
                }

                return (user, password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user info for email: {Email}", email);
                return (null, null);
            }
        }

        public void RemoveOtp(string email)
        {
            try
            {
                _memoryCache.Remove(email);
                _logger.LogDebug("OTP removed from cache for email: {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing OTP for email: {Email}", email);
            }
        }

        public void RemoveUserInfo(string email)
        {
            try
            {
                _memoryCache.Remove(email + "_user");
                _memoryCache.Remove(email + "_password");
                _logger.LogDebug("User info removed from cache for email: {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user info for email: {Email}", email);
            }
        }

        // ✅ BONUS: Get remaining OTP time
        public TimeSpan? GetOtpRemainingTime(string email)
        {
            try
            {
                if (_memoryCache.TryGetValue(email, out _))
                {
                    // Unfortunately, MemoryCache doesn't provide direct access to expiration time
                    // You would need to store timestamp separately if this feature is needed
                    return null;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OTP remaining time for email: {Email}", email);
                return null;
            }
        }
    }
}