using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.BillingItemFeat.DeleteCommands
{
    public record HardDeleteBillItemComand (int Id) : IRequest<Result<string>>;

    public class HardDeleteBillItemCommandHandler : IRequestHandler<HardDeleteBillItemComand, Result<string>>
    {
        private readonly IBillingRepository _billingRepository;
        public HardDeleteBillItemCommandHandler(IBillingRepository billingRepository)
        {
            _billingRepository = billingRepository;
        }
        public async Task<Result<string>> Handle(HardDeleteBillItemComand request, CancellationToken cancellationToken)
        {
            var bill = await _billingRepository.FindByIdAsync(request.Id);

            if (bill == null)
            {
                return Result<string>.Failure("Bill Item id not found");

            }
            await _billingRepository.RemoveAsync(bill, cancellationToken);
            await _billingRepository.SaveChangesAsync(cancellationToken);

            return Result<string>.Success("Bill Item hard deleted successfully");
        }
    }
}
