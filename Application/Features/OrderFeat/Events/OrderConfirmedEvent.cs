using Domain.Entities;
using MediatR;

namespace Application.Features.OrderFeat.Events;

public class OrderConfirmedEvent : INotification
{
    public Order Order { get; set; }
    public User User { get; set; }
    public int EtaMinutes{ get; set; }
}
