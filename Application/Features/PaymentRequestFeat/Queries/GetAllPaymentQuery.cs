using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.PaymentRequestFeat.Queries
{
    public record GetAllPaymentQuery(
            int PageNumber,
            int PageSize
        ) : IRequest<Result<IEnumerable<PaymentRequestDTO>>>;

    public class GetAllPaymentQueryHandler : IRequestHandler<GetAllPaymentQuery, Result<IEnumerable<PaymentRequestDTO>>>
    {
        private readonly IPaymentRequestRepository _paymentRequestRepository;
        public GetAllPaymentQueryHandler(IPaymentRequestRepository paymentRequestRepository)
        {
            _paymentRequestRepository = paymentRequestRepository;
            
        }
        public async Task<Result<IEnumerable<PaymentRequestDTO>>> Handle(GetAllPaymentQuery request, CancellationToken cancellationToken)
        {
            var payment = await _paymentRequestRepository.GetAllAsync(
                orderBy: query => query.OrderByDescending(payment => payment.Id),
                skip: (request.PageNumber - 1) * request.PageSize,
                take: request.PageSize,
                cancellationToken:cancellationToken

                );

             var paymentDto = payment.Select(p=>p.ToDTO()).ToList();

            return  Result<IEnumerable<PaymentRequestDTO>>.Success(paymentDto, "Payments request fetched successfully");
        }
    }

}
