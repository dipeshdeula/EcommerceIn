using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.PaymentMethodFeat.Queries
{
    public record GetAllPaymentMethodQuery(
        int PageNumber,
        int PageSize
        ) : IRequest<Result<IEnumerable<PaymentMethodDTO>>>;

    public class GetAllPaymentMethodQueryHanlder : IRequestHandler<GetAllPaymentMethodQuery, Result<IEnumerable<PaymentMethodDTO>>>
    {
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        public GetAllPaymentMethodQueryHanlder(IPaymentMethodRepository paymentMethodRepository)
        {
            _paymentMethodRepository = paymentMethodRepository;
        }
        public async Task<Result<IEnumerable<PaymentMethodDTO>>> Handle(GetAllPaymentMethodQuery request, CancellationToken cancellationToken)
        {
            var paymentMethods = await _paymentMethodRepository.GetAllAsync(
                orderBy: query => query.OrderByDescending(paymentMethods => paymentMethods.Id),
                skip: (request.PageNumber - 1) * request.PageSize,
                take: request.PageSize
                );
            var paymentMethodDTOs = paymentMethods.Select(pm => pm.ToDTO()).ToList();

            return Result<IEnumerable<PaymentMethodDTO>>.Success(paymentMethodDTOs, "Payment method fetched successfully");
        }
    }
}
