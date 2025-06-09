using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.OrderFeat.Events
{
    public class OrderPlacedEvent : INotification
    {
        public Order Order { get; set; }
        public User User { get; set; }
        public string[] ProductNames { get; set; }
    }
}
