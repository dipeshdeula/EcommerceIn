using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Infrastructure.Middleware.Security
{
    public class BusinessSecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BusinessSecurityMiddleware> _logger;

        // Track different types of operations separately
        private static readonly ConcurrentDictionary<string, List<DateTime>> _requestTracker = new();
        private static readonly ConcurrentDictionary<string, DateTime> _lastRegistrationTime = new();

        public BusinessSecurityMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            ILogger<BusinessSecurityMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_configuration.GetValue<bool>("SecuritySettings:EnableBusinessSecurity", false))
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value?.ToLower() ?? "";
            var method = context.Request.Method;
            var userIP = GetClientIP(context);

            // Handle different endpoint types
            if (IsRegistrationEndpoint(path, method))
            {
                var registrationCheck = await CheckRegistrationSecurity(userIP, context);
                if (!registrationCheck.IsAllowed)
                {
                    await WriteSecurityResponse(context, registrationCheck.Reason, 429);
                    return;
                }
            }
            else if (IsLoginEndpoint(path, method))
            {
                var loginCheck = CheckLoginSecurity(userIP);
                if (!loginCheck.IsAllowed)
                {
                    await WriteSecurityResponse(context, loginCheck.Reason, 429);
                    return;
                }
            }
            else if (ShouldProtectEndpoint(path, method))
            {
                var generalCheck = CheckGeneralRateLimit(path, userIP);
                if (!generalCheck.IsAllowed)
                {
                    await WriteSecurityResponse(context, generalCheck.Reason, 429);
                    return;
                }
            }

            await _next(context);
        }

        private async Task<(bool IsAllowed, string Reason)> CheckRegistrationSecurity(string userIP, HttpContext context)
        {
            try
            {
                // 1. Check time between registrations from same IP
                var lastRegKey = $"last_reg_{userIP}";
                var minTimeBetween = _configuration.GetValue<int>("SecuritySettings:RateLimiting:AccountOperations:AccountCreationRules:MinTimeBetweenRegistrationsSeconds", 30);

                if (_lastRegistrationTime.TryGetValue(lastRegKey, out var lastTime))
                {
                    if (DateTime.UtcNow - lastTime < TimeSpan.FromSeconds(minTimeBetween))
                    {
                        return (false, $"Please wait {minTimeBetween} seconds between registration attempts.");
                    }
                }

                // 2. Check hourly registration limit
                var hourlyLimit = _configuration.GetValue<int>("SecuritySettings:RateLimiting:AccountOperations:MaxRegistrationsPerIP:PerHour", 10);
                var hourlyCheck = CheckRegistrationLimit(userIP, "hour", hourlyLimit, TimeSpan.FromHours(1));

                if (!hourlyCheck.IsAllowed)
                {
                    return (false, $"Maximum {hourlyLimit} registrations per hour from your connection. This helps prevent spam accounts.");
                }

                // 3. Check daily registration limit  
                var dailyLimit = _configuration.GetValue<int>("SecuritySettings:RateLimiting:AccountOperations:MaxRegistrationsPerIP:PerDay", 25);
                var dailyCheck = CheckRegistrationLimit(userIP, "day", dailyLimit, TimeSpan.FromDays(1));

                if (!dailyCheck.IsAllowed)
                {
                    return (false, $"Maximum {dailyLimit} registrations per day from your connection. If you're in an office/cafe, please try again tomorrow or contact support.");
                }

                // 4. Update last registration time
                _lastRegistrationTime[lastRegKey] = DateTime.UtcNow;

                return (true, "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking registration security for IP {IP}", userIP);
                return (true, ""); // Fail open for errors
            }
        }

        private (bool IsAllowed, string Reason) CheckRegistrationLimit(string userIP, string timeFrame, int limit, TimeSpan window)
        {
            var key = $"reg_{userIP}_{timeFrame}";
            var now = DateTime.UtcNow;

            var requests = _requestTracker.GetOrAdd(key, _ => new List<DateTime>());

            lock (requests)
            {
                // Remove old requests
                requests.RemoveAll(r => now - r > window);

                // Check limit
                if (requests.Count >= limit)
                {
                    return (false, $"Registration limit exceeded for {timeFrame}");
                }

                // Add current request
                requests.Add(now);
                return (true, "");
            }
        }

        private (bool IsAllowed, string Reason) CheckLoginSecurity(string userIP)
        {
            var perMinuteLimit = _configuration.GetValue<int>("SecuritySettings:RateLimiting:AccountOperations:MaxLoginAttemptsPerIP:PerMinute", 5);
            var perHourLimit = _configuration.GetValue<int>("SecuritySettings:RateLimiting:AccountOperations:MaxLoginAttemptsPerIP:PerHour", 20);

            // Check per-minute limit
            var minuteCheck = CheckSimpleRateLimit(userIP, "login_min", perMinuteLimit, TimeSpan.FromMinutes(1));
            if (!minuteCheck.IsAllowed)
            {
                return (false, $"Too many login attempts. Please wait a minute before trying again.");
            }

            // Check per-hour limit  
            var hourCheck = CheckSimpleRateLimit(userIP, "login_hour", perHourLimit, TimeSpan.FromHours(1));
            if (!hourCheck.IsAllowed)
            {
                return (false, $"Too many failed login attempts. Please try again in an hour or reset your password.");
            }

            return (true, "");
        }

        private (bool IsAllowed, string Reason) CheckGeneralRateLimit(string path, string userIP)
        {
            var (limit, windowMinutes, reason) = GetRateLimitForPath(path);
            return CheckSimpleRateLimit(userIP, GetEndpointCategory(path), limit, TimeSpan.FromMinutes(windowMinutes));
        }

        private (bool IsAllowed, string Reason) CheckSimpleRateLimit(string userIP, string category, int limit, TimeSpan window)
        {
            var key = $"{userIP}_{category}";
            var now = DateTime.UtcNow;

            var requests = _requestTracker.GetOrAdd(key, _ => new List<DateTime>());

            lock (requests)
            {
                requests.RemoveAll(r => now - r > window);

                if (requests.Count >= limit)
                {
                    return (false, $"Rate limit exceeded for {category}");
                }

                requests.Add(now);
                return (true, "");
            }
        }

        // Helper methods
        private static bool IsRegistrationEndpoint(string path, string method)
        {
            return method == "POST" && (
                path.Contains("/auth/register") ||
                path.Contains("/account/register") ||
                path.Contains("/users/register") ||
                path.Contains("/signup")
            );
        }

        private static bool IsLoginEndpoint(string path, string method)
        {
            return method == "POST" && (
                path.Contains("/auth/login") ||
                path.Contains("/account/login") ||
                path.Contains("/signin")
            );
        }

        private bool ShouldProtectEndpoint(string path, string method)
        {
            var protectedEndpoints = new[]
            {
                "/cart", "/order", "/location/validate", "/products/search", "/payment"
            };

            return protectedEndpoints.Any(endpoint => path.Contains(endpoint));
        }

        private string GetEndpointCategory(string path)
        {
            if (path.Contains("/cart")) return "cart";
            if (path.Contains("/order")) return "order";
            if (path.Contains("/location")) return "location";
            if (path.Contains("/search")) return "search";
            return "general";
        }

        private (int Limit, int WindowMinutes, string Reason) GetRateLimitForPath(string path)
        {
            var config = _configuration.GetSection("SecuritySettings:RateLimiting");

            if (path.Contains("/cart"))
                return (config.GetValue<int>("CartOperations:MaxRequestsPerMinute", 15), 1,
                        "Too many cart operations");

            if (path.Contains("/order"))
                return (config.GetValue<int>("OrderOperations:MaxOrdersPerHour", 5), 60,
                        "Too many order attempts");

            if (path.Contains("/location"))
                return (config.GetValue<int>("LocationValidation:MaxValidationsPerMinute", 20), 1,
                        "Too many location requests");

            if (path.Contains("/search"))
                return (config.GetValue<int>("SearchOperations:MaxSearchesPerMinute", 30), 1,
                        "Too many searches");

            return (60, 1, "Rate limit exceeded");
        }

        private static string GetClientIP(HttpContext context)
        {
            return context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim() ??
                   context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
                   context.Connection.RemoteIpAddress?.ToString() ??
                   "127.0.0.1";
        }

        private static async Task WriteSecurityResponse(HttpContext context, string reason, int statusCode)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Security Limit Exceeded",
                message = reason,
                timestamp = DateTime.UtcNow,
                retryAfter = statusCode == 429 ? 60 : (int?)null
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }
    }
}
