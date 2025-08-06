using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.BillingItemFeat.DeleteCommands
{
    public record SoftDeleteBillingItemCommand (int Id) : IRequest<Result<string>>;

    public class SoftDeleteBillingItemCommandHandler : IRequestHandler<SoftDeleteBillingItemCommand, Result<string>>
    {
        private readonly IBillingRepository _billingRepository;
        public SoftDeleteBillingItemCommandHandler(IBillingRepository billingRepository)
        {
            _billingRepository = billingRepository;
        }
        public async Task<Result<string>> Handle(SoftDeleteBillingItemCommand request, CancellationToken cancellationToken)
        {
            var billingItems = await _billingRepository.FindByIdAsync(request.Id);
            if (billingItems == null)
            {
                return Result<string>.Failure("Billing Item Id not found");
            }

            await _billingRepository.SoftDeleteAsync(billingItems, cancellationToken);
            //await _billingRepository.SaveChangesAsync(cancellationToken);

            return Result<string>.Success("Billing Item soft deleted successfully");
        }
    }
}
