using Application.Interfaces.Repositories;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.ProductStoreFeat.Queries
{
    public class GetAllProductByStoreIdQueryValidator : AbstractValidator<GetAllProductByStoreIdQuery>
    {
        public GetAllProductByStoreIdQueryValidator(IStoreRepository storeRepository)
        {
            RuleFor(x => x.StoreId)
                .GreaterThan(0).WithMessage("StoreId must be greater than 0.")
                .MustAsync(async (id, _) =>
                    await storeRepository.AnyAsync(s => s.Id == id))
                .WithMessage("Store does not exist.");

            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("Page number must be greater than 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than 0.")
                .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100.");
        }
    }
}
