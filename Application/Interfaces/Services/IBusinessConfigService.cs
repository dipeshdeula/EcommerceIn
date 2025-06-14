using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IBusinessConfigService
    {
        string CompanyName { get; }
        string CompanyTagline { get; }
        string SupportEmail { get; }
        string SupportPhone { get; }
        string WebsiteUrl { get; }
        int DefaultDeliveryTimeMinutes { get; }
        string BusinessHoursStart { get; }
        string BusinessHoursEnd { get; }
        string BusinessTimezone { get; }
    }
}
