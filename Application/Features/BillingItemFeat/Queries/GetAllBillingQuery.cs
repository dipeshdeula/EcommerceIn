using Application.Common;
using Application.Dto.BilItemDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.BillingItemFeat.Queries
{
    public record GetAllBillingQuery (int PageNumber = 1, int PageSize = 10) : IRequest<Result<IEnumerable<BillingDTO>>>;

    public class GetAllBillingQueryHandler : IRequestHandler<GetAllBillingQuery, Result<IEnumerable<BillingDTO>>>
    {
        private readonly IBillingRepository _billingRepository;
        public GetAllBillingQueryHandler(IBillingRepository billingRepository)
        {
            _billingRepository = billingRepository;
        }

        public async Task<Result<IEnumerable<BillingDTO>>> Handle(GetAllBillingQuery request, CancellationToken cancellationToken)
        {
            var billings = await _billingRepository.GetAllAsync(
             orderBy: query => query.OrderByDescending(billings => billings.Id),
             includeProperties: "User,PaymentRequest,Order,CompanyInfo,Items",
             includeDeleted: false,
             skip: (request.PageNumber - 1) * request.PageSize,
             take: request.PageSize,
             cancellationToken: cancellationToken);

            var billingDto = billings.Select(b => b.ToDTO()).ToList();

            return Result<IEnumerable<BillingDTO>>.Success(billingDto, "Billing retrieved successfully");

        }
    }
    
}
