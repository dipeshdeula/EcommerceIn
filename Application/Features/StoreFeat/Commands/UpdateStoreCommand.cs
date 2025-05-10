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

namespace Application.Features.StoreFeat.Commands
{
    public record UpdateStoreCommand (int Id, string? Name,string? OwnerName) : IRequest<Result<StoreDTO>>;

    public class UpdateStoreCommandHandler : IRequestHandler<UpdateStoreCommand, Result<StoreDTO>>
    {
        private readonly IStoreRepository _storeRepository;

        public UpdateStoreCommandHandler(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;            
        }
        public async Task<Result<StoreDTO>> Handle(UpdateStoreCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.FindByIdAsync(request.Id);
            if (store == null || store.IsDeleted== true)
                return Result<StoreDTO>.Failure("Store id is not found");

            store.Name = request.Name ?? store.Name;
            store.OwnerName = request.OwnerName ?? store.OwnerName;

            await _storeRepository.UpdateAsync(store);

            return Result<StoreDTO>.Success(store.ToDTO(), "store update successfully");

        }
    }
}
