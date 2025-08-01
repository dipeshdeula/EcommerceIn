using Domain.Enums;

namespace Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty ;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;
        public UserRoles Role { get; set; } = UserRoles.User;

        // for google Auth
        public bool EmailVerified { get; set; } = false;
        public string? ExternalProvider { get; set; } = null; // e.g., "Google", "Facebook"
        public string? ExternalProviderId { get; set; } = null; // e.g., Google ID, Facebook ID

        public ICollection<Address> Addresses { get; set; } = new List<Address>();

    }
}
