using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.BillingItemFeat.DeleteCommands
{
    public record UnDeleteBilingItemCommand (int Id) : IRequest<Result<string>>;

    public class UnDeleteBillingItemCommandHandler : IRequestHandler<UnDeleteBilingItemCommand, Result<string>>
    {
        private readonly IBillingRepository _billingRepository;
        public UnDeleteBillingItemCommandHandler(IBillingRepository billingItemRepository)
        {
            _billingRepository = billingItemRepository;
        }
        public async Task<Result<string>> Handle(UnDeleteBilingItemCommand request, CancellationToken cancellationToken)
        {
            var billingItem = await _billingRepository.FindByIdAsync(request.Id);

            if (billingItem == null)
            {
                return Result<string>.Failure("Billing Item Id not found");
                               
            }

            await _billingRepository.UndeleteAsync(billingItem, cancellationToken);
            //await _billingRepository.SaveChangesAsync(cancellationToken);

            return Result<string>.Success("Billing Item undeleted successfully");
        }
    }
}
