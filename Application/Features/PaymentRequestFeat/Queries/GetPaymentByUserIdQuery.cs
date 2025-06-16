using Application.Common;
using Application.Common.Models;
using Application.Dto.PaymentDTOs;
using Application.Extension;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentRequestFeat.Queries
{
    public record GetPaymentByUserIdQuery(
        int UserId,
        int PageNumber = 1,
        int PageSize = 10,
        string? Status = null,
        string? OrderBy = "CreatedAt"
        ) : IRequest<Result<IEnumerable<PaymentRequestDTO>>>;

    public class GetPaymentByUserIdQueryHandler : IRequestHandler<GetPaymentByUserIdQuery, Result<IEnumerable<PaymentRequestDTO>>>
    {
        //private readonly IPaymentRequestRepository _paymentRequestRepository;
        private readonly IUnitOfWork _unitOfWork;
        public GetPaymentByUserIdQueryHandler(
            IUnitOfWork unitOfWork
            )
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<Result<IEnumerable<PaymentRequestDTO>>> Handle(GetPaymentByUserIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // validate user exists
                var userExists = await _unitOfWork.Users.FindByIdAsync(request.UserId);

                if (userExists == null)
                {
                    return Result<IEnumerable<PaymentRequestDTO>>.Failure($"User with ID {request.UserId} not found");

                }

                var query = _unitOfWork.PaymentRequests.GetQueryable()
                    .Include(pr => pr.User)
                    .Include(pr => pr.Order)
                    .Include(pr => pr.PaymentMethod)
                    .Where(pr => pr.UserId == request.UserId);

                // Apply status filter if provided
                if (!string.IsNullOrEmpty(request.Status))
                {
                    query = query.Where(pr => pr.PaymentStatus.ToLower() == request.Status.ToLower());
                }

                // Apply ordering
                query = request.OrderBy?.ToLower() switch
                {
                    "amount" => query.OrderByDescending(pr => pr.PaymentAmount),
                    "status" => query.OrderBy(pr => pr.PaymentStatus),
                    "updatedat" => query.OrderByDescending(pr => pr.UpdatedAt),
                    _ => query.OrderByDescending(pr => pr.CreatedAt)

                };

                // get total count for pagination
                var totalCount = await query.CountAsync(cancellationToken);

                // Apply pagination
                var payments = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                // Map to DTOs
                var paymentDtos = payments.Select(p => p.ToDTO()).ToList();
        

                return Result<IEnumerable<PaymentRequestDTO>>.Success(
                    paymentDtos,
                    $"Retrieved {paymentDtos.Count} payment(s) for user {request.UserId}",
                    totalCount,
                    request.PageNumber,
                    request.PageSize
                    );
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<PaymentRequestDTO>>.Failure(
                                        $"Failed to retrieve payments for user {request.UserId}: {ex.Message}"

                    );
            }

        }
    }
}
