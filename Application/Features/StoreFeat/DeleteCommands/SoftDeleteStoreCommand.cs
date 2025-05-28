using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.StoreFeat.DeleteCommands
{
    public record SoftDeleteStoreCommand (int Id) : IRequest<Result<StoreDTO>>;

    public class SoftDeleteStoreCommandHandler : IRequestHandler<SoftDeleteStoreCommand, Result<StoreDTO>>
    {
        private readonly IStoreRepository _storeRepository;
        public SoftDeleteStoreCommandHandler(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;            
        }
        public async Task<Result<StoreDTO>> Handle(SoftDeleteStoreCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.FindByIdAsync(request.Id);
            if (store == null)
            {
                return Result<StoreDTO>.Failure("Store id not found");
                
            }

            await _storeRepository.SoftDeleteAsync(store, cancellationToken);

            return Result<StoreDTO>.Success(store.ToDTO(), "store deleted successfully");

        }
    }
}
