using Application.Common;
using Application.Dto.PaymentDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentRequestFeat.Queries
{
    public record GetPaymentByUserIdQuery(
        int UserId, int PageNumber,int PageSize) : IRequest<Result<IEnumerable<PaymentRequestDTO>>>;

    public class GetPaymentByUserIdQueryHandler : IRequestHandler<GetPaymentByUserIdQuery, Result<IEnumerable<PaymentRequestDTO>>>
    {
        private readonly IPaymentRequestRepository _paymentRequestRepository;
        public GetPaymentByUserIdQueryHandler(IPaymentRequestRepository paymentRequestRepsository)
        {
            _paymentRequestRepository = paymentRequestRepsository;
        }
        public async Task<Result<IEnumerable<PaymentRequestDTO>>> Handle(GetPaymentByUserIdQuery request, CancellationToken cancellationToken)
        {
            // Fetch payment details associates with user
            var userPayments = await _paymentRequestRepository.GetQueryable()
                .Where(up => up.UserId == request.UserId).ToListAsync();
            var userPaymentDto = userPayments.Select(u => u.ToDTO()).ToList();
            return Result<IEnumerable<PaymentRequestDTO>>.Success(userPaymentDto,"Payment Details by user id has been fetched!");

        }
    }
}
