using Application.Common;
using Application.Dto.StoreDTOs;
using Application.Enums;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.StoreFeat.Commands
{
    public record CreateStoreCommand(        
        string Name,
        string OwnerName,
        IFormFile File
        ) : IRequest<Result<StoreDTO>>;

    public class CreateStoreCommandHandler : IRequestHandler<CreateStoreCommand, Result<StoreDTO>>
    {
        private readonly IStoreRepository _storeRepository;
        private readonly IFileServices _fileService;
        public CreateStoreCommandHandler(IStoreRepository storeRepository,IFileServices fileService)
        {
            _storeRepository = storeRepository;
            _fileService = fileService;

        }
        public async Task<Result<StoreDTO>> Handle(CreateStoreCommand request, CancellationToken cancellationToken)
        {
            string fileUrl = null;
            if (request.File != null && request.File.Length > 0)
            {
                try
                {
                    fileUrl = await _fileService.SaveFileAsync(request.File, FileType.StoreImages);
                }
                catch (Exception ex)
                {
                    return Result<StoreDTO>.Failure($"Image upload failed:{ex.Message}");
                }
            }
            var store = new Store
            {
               
                Name = request.Name,
                OwnerName = request.OwnerName,
                ImageUrl = fileUrl

            };
            var storeDto = new StoreDTO
            {
                Id = store.Id,
                Name = store.Name,
                OwnerName = store.OwnerName,
                ImageUrl = fileUrl,
                IsDeleted = store.IsDeleted,
                //Address = store.Address
            };

            var createStore = await _storeRepository.AddAsync(store, cancellationToken);
            await _storeRepository.SaveChangesAsync(cancellationToken);
            if (createStore == null)
            {
                return Result<StoreDTO>.Failure("Failed to create store");
            }

            return Result<StoreDTO>.Success(createStore.ToDTO(), "Store created successfully");

        }
    }

}
