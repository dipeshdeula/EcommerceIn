using Application.Common;
using Application.Dto.BilItemDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.AddressFeat.Queries
{
    public record GetBillByUserIdQuery(
        int UserId
        ) : IRequest<Result<IEnumerable<BillingDTO>>>;

    public class GetBillByUserIdQueryHandler : IRequestHandler<GetBillByUserIdQuery, Result<IEnumerable<BillingDTO>>>
    {
        private readonly IBillingRepository _billingRepository;
        private readonly IUserRepository _userRepository;
        public GetBillByUserIdQueryHandler(IBillingRepository billingRepository,IUserRepository userRepository)
        {
            _billingRepository = billingRepository;
            _userRepository = userRepository;
        }
        public async Task<Result<IEnumerable<BillingDTO>>> Handle(GetBillByUserIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userRepository.FindByIdAsync(request.UserId);
                if (user == null)
                    return Result<IEnumerable<BillingDTO>>.Failure("UserId not found");
                var billings = await _billingRepository.GetAllAsync(
                 predicate : (b => b.UserId == user.Id),
                 orderBy: query => query.OrderByDescending(billings => billings.Id),
                 includeProperties: "User,PaymentRequest,Order,CompanyInfo,Items",
                 includeDeleted: false,                 
                 cancellationToken: cancellationToken);

                var billingList = billings.Select(b => b.ToDTO()).ToList();

                return Result<IEnumerable<BillingDTO>>.Success(billingList, "Bill info retreive successfully");

            }
            catch (Exception ex)
            {
                return Result<IEnumerable<BillingDTO>>.Failure("Unable to retreive bill info",ex.Message);
            }
           


        }
    }

}
