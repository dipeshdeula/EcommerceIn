using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Application.Features.ProductStoreFeat.Queries
{
    public record GetAllProductByStoreIdQuery
        (int  StoreId, int PageNumber,int PageSize) : IRequest<Result<IEnumerable<StoreWithProductsDTO>>>;

    public class GetAllProductByStoreIdQueryHandler : IRequestHandler<GetAllProductByStoreIdQuery, Result<IEnumerable<StoreWithProductsDTO>>>
    {
        private readonly IProductStoreRepository _productStoreRepository;
        public GetAllProductByStoreIdQueryHandler(IProductStoreRepository productStoreRepository)
        {
            _productStoreRepository = productStoreRepository;
            
        }
        public async Task<Result<IEnumerable<StoreWithProductsDTO>>> Handle(GetAllProductByStoreIdQuery request, CancellationToken cancellationToken)
        {
            // Fetch products and their associated store
            var productStores = await _productStoreRepository.GetQueryable()
                .Where(ps => ps.StoreId == request.StoreId && !ps.IsDeleted)     
                .Include(ps => ps.Product)
                .Include(ps=>ps.Product.Images)
                .Include(ps => ps.Store) // Include Store details
                .ThenInclude(store=>store.Address)                 
                .ToListAsync(cancellationToken);       

            // Group products by store
            var groupedData = productStores
                .GroupBy(ps => ps.Store)
                .Select(group => new StoreWithProductsDTO
                {
                    Store = group.Key.ToDTO(), // Map Store to StoreDTO
                    Products = group.Select(ps => ps.Product.ToDTO()).ToList() // Map Products to ProductDTO
                })
                .ToList();

            return Result<IEnumerable<StoreWithProductsDTO>>.Success(groupedData, "Products fetched successfully");
        }
    }

}
