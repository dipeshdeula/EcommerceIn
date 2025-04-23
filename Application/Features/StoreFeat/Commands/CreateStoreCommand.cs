using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;

namespace Application.Features.StoreFeat.Commands
{
    public record CreateStoreCommand(
        int Id,
        string Name,
        string OwnerName
        ) : IRequest<Result<StoreDTO>>;

    public class CreateStoreCommandHandler : IRequestHandler<CreateStoreCommand, Result<StoreDTO>>
    {
        private readonly IStoreRepository _storeRepository;
        public CreateStoreCommandHandler(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;

        }
        public async Task<Result<StoreDTO>> Handle(CreateStoreCommand request, CancellationToken cancellationToken)
        {
            var store = new Store
            {
                Id = request.Id,
                Name = request.Name,
                OwnerName = request.OwnerName

            };
            var storeDto = new StoreDTO
            {
                Id = store.Id,
                Name = store.Name,
                OwnerName = store.OwnerName,
                IsDeleted = store.IsDeleted,
                Address = store.Address
            };

            var createStore = await _storeRepository.AddAsync(store, cancellationToken);
            if (createStore == null)
            {
                return Result<StoreDTO>.Failure("Failed to create store");
            }

            return Result<StoreDTO>.Success(createStore.ToDTO(), "Store created successfully");

        }
    }

}
