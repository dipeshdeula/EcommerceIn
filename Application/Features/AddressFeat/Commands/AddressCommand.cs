/*using Application.Common;
using Domain.Entities;
using MediatR;
using Microsoft.IdentityModel.Tokens;

namespace Application.Features.AddressFeat.Commands
{
    public record AddressCommand(
        int Id,
        int userId,
        string label,
        string street, 
        string city,
        string province,
        string postalCode, 
        string latitude,
        string longitude,
        bool isDefault) : IRequest<Result<Address>>;

    public class AddressCommandHandler : IRequestHandler<AddressCommand, Result<Address>>
    {
        private readonly
       public Task<Result<Address>> Handle(AddressCommand request, CancellationToken cancellationToken)
        {
          
        }
    }


}
*/