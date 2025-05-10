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
    public record UnDeleteStoreCommand(int Id) : IRequest<Result<StoreDTO>>;

    public class UnDeleteStoreCommandHandler : IRequestHandler<UnDeleteStoreCommand, Result<StoreDTO>>
    {
        private readonly IStoreRepository _storeRepository;
        public UnDeleteStoreCommandHandler(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
            
        }
        public async Task<Result<StoreDTO>> Handle(UnDeleteStoreCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.FindByIdAsync(request.Id);
            if (store == null)
                return Result<StoreDTO>.Failure("Store is not found");
            await _storeRepository.UndeleteAsync(store, cancellationToken);

            return Result<StoreDTO>.Success(store.ToDTO(), "store is undeleted successfully");
        }
    }
}
