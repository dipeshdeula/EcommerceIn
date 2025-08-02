using Application.Common;
using Application.Dto.CategoryDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.Queries
{
    public record GetAllProductsBySubSubCategoryIdQuery(
        int SubSubCategoryId,
        int PageNumber,
        int PageSize
        ) : IRequest<Result<CategoryWithProductsDTO>>;

    public class GetAllProductsBySubSubCategoryIdQueryHandler : IRequestHandler<GetAllProductsBySubSubCategoryIdQuery, Result<CategoryWithProductsDTO>>
    {
       
        private readonly ISubSubCategoryRepository _subSubCategoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IProductPricingService _pricingService;
        public GetAllProductsBySubSubCategoryIdQueryHandler(
           
            ISubSubCategoryRepository subSubCategoryRepository,
            IProductRepository productRepository
            , IProductPricingService pricingService
            )
        {            
            _subSubCategoryRepository = subSubCategoryRepository;
            _productRepository = productRepository;
            _pricingService = pricingService;
            
        }
        public async Task<Result<CategoryWithProductsDTO>> Handle(GetAllProductsBySubSubCategoryIdQuery request, CancellationToken cancellationToken)
        {
            var subSubCategory = await _subSubCategoryRepository.FindByIdAsync(request.SubSubCategoryId);
            if (subSubCategory == null)
            {
                return Result<CategoryWithProductsDTO>.Failure($"SubSubCategory Id : {request.SubSubCategoryId} not fuound");
            }

            var getProducts = await _subSubCategoryRepository.GetProductsBySubSubCategoryIdAsync(subSubCategory.Id, request.PageNumber, request.PageSize);

            // Convert to DTOs
            var productDTOs = getProducts.Select(p => p.ToDTO()).ToList();

            // Apply dynamic pricing to all products
            await productDTOs.ApplyPricingAsync(_pricingService,null);

            var categoryWithProductsDto = new CategoryWithProductsDTO
            {
                Id = subSubCategory.Id,
                Name = subSubCategory.Name,
                Slug = subSubCategory.Slug,
                Description = subSubCategory.Description,
                Products = productDTOs,

                //  Pricing statistics
                TotalProducts = productDTOs.Count,
                ProductsOnSale = productDTOs.Count(p => p.IsOnSale),
                AveragePrice = productDTOs.Any() ? productDTOs.Average(p => p.Pricing.EffectivePrice) : 0,
                MinPrice = productDTOs.Any() ? productDTOs.Min(p => p.Pricing.EffectivePrice) : 0,
                MaxPrice = productDTOs.Any() ? productDTOs.Max(p => p.Pricing.EffectivePrice) : 0,
                TotalSavings = productDTOs.Sum(p => p.Pricing.ProductDiscountAmount + p.Pricing.EventDiscountAmount)
            };
            return Result<CategoryWithProductsDTO>.Success(categoryWithProductsDto,
                               $"SubSubCategory retrieved with {categoryWithProductsDto.ProductsOnSale} items on sale. Total potential savings: Rs.{categoryWithProductsDto.TotalSavings:F2}");


        }
    }

}
