using Application.Common;
using Application.Dto.OrderDTOs;
using Domain.Entities;
using MediatR;
using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.OrderFeat.Events
{
    public class OrderCancelledEvents : INotification
    { 
        public Order Order { get; set; }
        public User User { get; set; }
    }


    
}
