using Application.Common;
using Application.Dto;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.UpdateCommands
{
    public record UpdateProductCommand(
        int ProductId,
        string Name,
        string Slug,
        string Description
        ) : IRequest<Result<ProductDTO>>;

    public class UpdateProudctComamndHandler : IRequestHandler<UpdateProductCommand, Result<ProductDTO>>
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<UpdateProductCommand> _logger;

        public UpdateProudctComamndHandler(IProductRepository productRepository, ILogger<UpdateProductCommand> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
            
        }

        public async Task<Result<ProductDTO>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.FindByIdAsync(request.ProductId);
            if (product == null)
            {
                return Result<ProductDTO>.Failure("Product Id not found");
            }

            string name = request.Name ?? product.Name;
            string slug = request.Slug ?? product.Slug;
            string Description = request.Slug ?? product.Description;

            await _productRepository.UpdateAsync(product, cancellationToken);

            var productDto = new ProductDTO
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                Description = product.Description
            };

            return Result<ProductDTO>.Success(productDto, "Product updated successfully");
        }
    }
    
}
