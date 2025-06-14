using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.Shared
{
    public class NotificationResultDTO
    {
        public bool EmailSent { get; set; }
        public string? EmailError { get; set; }
        public bool RabbitMqSent { get; set; }
        public string? RabbitMqError { get; set; }

        public bool AllNotificationsSent => EmailSent && RabbitMqSent;
        public bool AnyNotificationSent => EmailSent || RabbitMqSent;

        public string NotificationSummary
        {
            get
            {
                if (AllNotificationsSent) return "All notifications sent successfully";
                if (AnyNotificationSent) return "Some notifications sent (check details)";
                return "No notifications sent";
            }
        }
    }
}
