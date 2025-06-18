using Application.Common;
using Application.Dto.BilItemDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.BillingItemFeat.Queries
{
    public record GetAllBillingItemQuery (int PageNumber,int PageSize) : IRequest<Result<IEnumerable<BillingItemDTO>>>;

    public class GetAllBillingItemQueryHandler : IRequestHandler<GetAllBillingItemQuery, Result<IEnumerable<BillingItemDTO>>>
    {
        private readonly IBillingItemRepository _billingItemRepository;
        public GetAllBillingItemQueryHandler(IBillingItemRepository billingItemRepository)
        {
            _billingItemRepository = billingItemRepository;
        }
        public async Task<Result<IEnumerable<BillingItemDTO>>> Handle(GetAllBillingItemQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var billings = await _billingItemRepository.GetAllAsync(
                    skip: (request.PageNumber - 1) * request.PageSize,
                    take: request.PageSize,
                    includeDeleted: false,
                    cancellationToken: cancellationToken

                    );

                var billingsDto = billings.Select(b => b.ToDTO()).ToList();

                return Result<IEnumerable<BillingItemDTO>>.Success(billingsDto, "Bill info fetched successfully");
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<BillingItemDTO>>.Failure("Unable to fetched billing info");
            }

               
        }
    }

}
