using Application.Common;
using Application.Dto.StoreDTOs;
using Application.Enums;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.StoreFeat.Commands
{
    public record UpdateStoreCommand (
         int Id, 
         string? Name,
         string? OwnerName,
        IFormFile? FIle) : IRequest<Result<StoreDTO>>;

    public class UpdateStoreCommandHandler : IRequestHandler<UpdateStoreCommand, Result<StoreDTO>>
    {
        private readonly IStoreRepository _storeRepository;
        private readonly IFileServices _fileService;

        public UpdateStoreCommandHandler(IStoreRepository storeRepository,IFileServices fileService)
        {
            _storeRepository = storeRepository;
            _fileService = fileService;
        }
        public async Task<Result<StoreDTO>> Handle(UpdateStoreCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.FindByIdAsync(request.Id);
            if (store == null || store.IsDeleted== true)
                return Result<StoreDTO>.Failure("Store id is not found");

            store.Name = request.Name ?? store.Name;
            store.OwnerName = request.OwnerName ?? store.OwnerName;

            // Handle image update
            if (request.FIle != null)
            {
                try
                {
                    store.ImageUrl = await _fileService.UpdateFileAsync(store.ImageUrl, request.FIle, FileType.StoreImages);
                }
                catch (Exception ex)
                {
                    return Result<StoreDTO>.Failure("Image update failed");
                }
            }

            await _storeRepository.UpdateAsync(store);
            await _storeRepository.SaveChangesAsync(cancellationToken);

            return Result<StoreDTO>.Success(store.ToDTO(), "store update successfully");

        }
    }
}
