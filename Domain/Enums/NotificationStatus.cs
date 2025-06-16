using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums;

public enum NotificationStatus
{
    Pending = 0,  //Queued
    Sent = 1, //sent to the signalR hub
    Delivered = 2, //reached client device
    Failed = 3, //couldn't be sent due to error
    Read = 4, //user opened the notification
    Acknowledged = 5, //user acknowledged the notification
}
