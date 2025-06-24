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
    public string Title { get; set; }
    public string Message { get; set; }
    public string Type { get; set; }
    public string Status { get; set; }
    public bool IsRead { get; set; }
    public DateTime OrderDate { get; set; }
}
