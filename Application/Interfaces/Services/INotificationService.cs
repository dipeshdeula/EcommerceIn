﻿using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public interface INotificationService
{
    Task SendToUserAsync(string Email, Notification notification);
    Task SendToAllAsync();
}
