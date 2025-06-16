using Application.Common;
using Application.Common.Models;
using Application.Dto.PaymentDTOs;
using Application.Extension;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentRequestFeat.Queries
{
    public record GetAllPaymentQuery(
        int PageNumber = 1,
        int PageSize = 10,
        string? Status = null, 
        int? PaymentMethodId = null, 
        DateTime? FromDate = null, 
        DateTime? ToDate = null,
        string? SearchTerm = null, 
        string? OrderBy = "CreatedAt" 
    ) : IRequest<Result<IEnumerable<PaymentRequestDTO>>>;

    public class GetAllPaymentQueryHandler : IRequestHandler<GetAllPaymentQuery, Result<IEnumerable<PaymentRequestDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllPaymentQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<IEnumerable<PaymentRequestDTO>>> Handle(GetAllPaymentQuery request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.PageNumber < 1)
                {
                    return Result<IEnumerable<PaymentRequestDTO>>.Failure("Page number must be greater than 0");
                }

                if (request.PageSize < 1 || request.PageSize > 100)
                {
                    return Result<IEnumerable<PaymentRequestDTO>>.Failure("Page size must be between 1 and 100");
                }

                var query = _unitOfWork.PaymentRequests.GetQueryable()
                    .Include(pr => pr.User)
                    .Include(pr => pr.Order)
                    .Include(pr => pr.PaymentMethod)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(request.Status))
                {
                    query = query.Where(pr => pr.PaymentStatus.ToLower() == request.Status.ToLower());
                }

                if (request.PaymentMethodId.HasValue)
                {
                    query = query.Where(pr => pr.PaymentMethodId == request.PaymentMethodId.Value);
                }

                if (request.FromDate.HasValue)
                {
                    query = query.Where(pr => pr.CreatedAt >= request.FromDate.Value);
                }

                if (request.ToDate.HasValue)
                {
                    query = query.Where(pr => pr.CreatedAt <= request.ToDate.Value);
                }

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    var searchLower = request.SearchTerm.ToLower();
                    query = query.Where(pr =>
                        pr.User.Name.ToLower().Contains(searchLower) ||
                        pr.Description.ToLower().Contains(searchLower) ||
                        pr.EsewaTransactionId.ToLower().Contains(searchLower) ||
                        pr.KhaltiPidx.ToLower().Contains(searchLower));
                }

                query = request.OrderBy?.ToLower() switch
                {
                    "amount" => query.OrderByDescending(pr => pr.PaymentAmount),
                    "amount_asc" => query.OrderBy(pr => pr.PaymentAmount),
                    "status" => query.OrderBy(pr => pr.PaymentStatus),
                    "user" => query.OrderBy(pr => pr.User.Name),
                    "method" => query.OrderBy(pr => pr.PaymentMethod.Name),
                    "updatedat" => query.OrderByDescending(pr => pr.UpdatedAt),
                    "updatedat_asc" => query.OrderBy(pr => pr.UpdatedAt),
                    "createdat_asc" => query.OrderBy(pr => pr.CreatedAt),
                    _ => query.OrderByDescending(pr => pr.CreatedAt) // Default: newest first
                };

                var totalCount = await query.CountAsync(cancellationToken);

                var payments = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                var paymentDtos = payments.Select(p => p.ToDTO()).ToList();
               

                var message = $"Retrieved {paymentDtos.Count} payment(s) from {totalCount} total records";
                if (!string.IsNullOrEmpty(request.Status))
                {
                    message += $" (filtered by status: {request.Status})";
                }

                return Result<IEnumerable<PaymentRequestDTO>>.Success(paymentDtos, message,totalCount,request.PageNumber,request.PageSize);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<PaymentRequestDTO>>.Failure(
                    $"Failed to retrieve payments: {ex.Message}"
                );
            }
        }
    }
}