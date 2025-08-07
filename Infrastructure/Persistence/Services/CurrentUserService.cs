using Domain.Enums;
using System.Security.Claims;

namespace Infrastructure.Persistence.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public string? UserEmail => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

        public string? UserName => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public string? GetUserIp => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;

        public bool IsAdmin => IsAuthenticated && (
            _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true ||
            _httpContextAccessor.HttpContext?.User?.IsInRole("SuperAdmin") == true ||
            _httpContextAccessor.HttpContext?.User?.HasClaim("role", "Admin") == true ||
            _httpContextAccessor.HttpContext?.User?.HasClaim("role", "SuperAdmin") == true);

        public bool IsVendor => IsAuthenticated && (
            _httpContextAccessor.HttpContext?.User?.IsInRole("Vendor") == true ||
            _httpContextAccessor.HttpContext?.User?.HasClaim("role", "Vendor") == true);

        public bool CanManageProducts => IsAdmin || IsVendor;

        public int? GetUserIdAsInt()
        {
            if (IsAuthenticated && int.TryParse(UserId, out int id))
            {
                return id;
            }
            return null;
        }
    }
}
