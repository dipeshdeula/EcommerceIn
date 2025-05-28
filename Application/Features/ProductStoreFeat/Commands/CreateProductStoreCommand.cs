using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.ProductStoreFeat.Commands
{
    public record CreateProductStoreCommand (int StoreId,int ProductId) : IRequest<Result<ProductStoreDTO>>;

    
    public class CreateProductStoreCommandHandler : IRequestHandler<CreateProductStoreCommand, Result<ProductStoreDTO>>
    {
        private readonly IStoreRepository _storeRepository;
        private readonly IProductRepository _productRepository;
        private readonly IProductStoreRepository _productStoreRepository;
        public CreateProductStoreCommandHandler(IStoreRepository storeRepository,
            IProductStoreRepository productStoreRepository,
            IProductRepository productRepository)
        {
            _storeRepository = storeRepository;
            _productStoreRepository = productStoreRepository;
            _productRepository = productRepository;

        }
        public async Task<Result<ProductStoreDTO>> Handle(CreateProductStoreCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.FindByIdAsync(request.StoreId);
            if (store == null)
            {
                return Result<ProductStoreDTO>.Failure("Store not found.");
            }
            var product = await _productRepository.FindByIdAsync(request.ProductId);
            if (product == null)
            {
                return Result<ProductStoreDTO>.Failure("Product not found.");
            }

            // Check if the ProductStore relationShip already exists
            var existingProductStore = await _productStoreRepository.FirstOrDefaultAsync(
                 ps => ps.ProductId == request.ProductId && ps.StoreId == request.StoreId);
            if (existingProductStore != null)
            {
                return Result<ProductStoreDTO>.Failure("This product is already associated with the store.");
            }
            // Create a new ProductStore record
            var productStore = new ProductStore
            {
                ProductId = request.ProductId,
                StoreId = request.StoreId,
                IsDeleted = false,
                Product = product,
                Store = store
            };
            await _productStoreRepository.AddAsync(productStore);
            await _productStoreRepository.SaveChangesAsync();

            return Result<ProductStoreDTO>.Success(productStore.ToDTO(), "ProductStore created successfully.");
        }
    }
}
