using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.PromoCodeFeat.DeleteCommands
{
    public record UnDeletePromoCodeCommand (int Id) : IRequest<Result<string>>;

    public class UnDeletePromoCodeCommandHandler : IRequestHandler<UnDeletePromoCodeCommand, Result<string>>
    {
        private readonly IPromoCodeRepository _promoCodeRepository;
        public UnDeletePromoCodeCommandHandler(IPromoCodeRepository promoCodeRepository)
        {
            _promoCodeRepository = promoCodeRepository;
            
        }

        public async Task<Result<string>> Handle(UnDeletePromoCodeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var Promos = await _promoCodeRepository.FindByIdAsync(request.Id);
                if (Promos == null)
                {
                    return Result<string>.Failure("Promo Id is not found");
                }

                await _promoCodeRepository.UndeleteAsync(Promos, cancellationToken);
                await _promoCodeRepository.SaveChangesAsync(cancellationToken);

                return Result<string>.Success("PromoCode Undeleted successfully");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure("error:",ex.Message);
            }           
                           

        }
    }
   
}
