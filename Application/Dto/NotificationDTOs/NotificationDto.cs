using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.NotificationDTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Email { get; set; }
    public int OrderId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime OrderDate { get; set; }
}
