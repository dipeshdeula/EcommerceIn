using Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities;
public class Notification 
{
    public int Id { get; set; }

    [NotMapped]
    public string Email { get; set; }
    public int UserId { get; set; }
    public int OrderId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public NotificationType Type { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? NextRetryAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    [JsonIgnore]
    public Order? Order { get; set; }
    [JsonIgnore]
    public User? User { get; set; }


    [ConcurrencyCheck]
    [Column("xmin")]
    public uint RowVersion { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
