using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;

namespace Application.Features.CategoryFeat.Commands
{
    public record CreateProductCommand(
        int subSubCategoryId,
        string Name,
        string Slug,
        string Description,
        decimal Price,
        decimal DiscountPrice,
        int StockQuantity

        ):IRequest<Result<ProductDTO>>;

    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<ProductDTO>>
    {
        private readonly ISubSubCategoryRepository _subSubCategoryRepository;
        public CreateProductCommandHandler(ISubSubCategoryRepository subSubCategoryRepository)
        {
            _subSubCategoryRepository = subSubCategoryRepository;
            
        }
        public async Task<Result<ProductDTO>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            // Validate SubSubCategoryId
            var subSubCategory = await _subSubCategoryRepository.FindByIdAsync(request.subSubCategoryId);
            if (subSubCategory == null)
            {
                return Result<ProductDTO>.Failure("sub-sub category not found");
            }

            // Create the new Product item
            var product = new Product
            {
                SubSubCategoryId = request.subSubCategoryId,
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Slug,
                Price = request.Price,
                DiscountPrice = request.DiscountPrice,
                StockQuantity = request.StockQuantity,
                SubSubCategory = subSubCategory

            };

            // Add the SubSubCategory to the parent's subsubcategories collection
            subSubCategory.Products.Add(product);

            // save changes to the database
            await _subSubCategoryRepository.UpdateAsync(subSubCategory, cancellationToken);

            // Map to the Dto and return success
            return Result<ProductDTO>.Success(product.ToDTO(), "Product item created successfully");
        }
    }

}
