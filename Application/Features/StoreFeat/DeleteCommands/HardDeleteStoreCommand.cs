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
    public record HardDeleteStoreCommand (int Id) : IRequest<Result<StoreDTO>>;

    public class HardDeleteStoreCommandHandler : IRequestHandler<HardDeleteStoreCommand, Result<StoreDTO>>
    {
        private readonly IStoreRepository _storeRepository;
        public HardDeleteStoreCommandHandler(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;            
        }
        public async Task<Result<StoreDTO>> Handle(HardDeleteStoreCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.FindByIdAsync(request.Id);
            if (store == null)
                return Result<StoreDTO>.Failure("Store is not found");
            await _storeRepository.RemoveAsync(store, cancellationToken);

            return Result<StoreDTO>.Success(store.ToDTO(), "store is deleted successfully");
        }
    }
}
