using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public string JwtId { get; set; } = string.Empty;
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime ExpiryDateTimeUtc { get; set; }

        public bool Used { get; set; }

        public bool Invalidated { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!; // Navigation property to User entity
    }
}
