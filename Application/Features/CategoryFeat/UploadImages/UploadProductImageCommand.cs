using Application.Common;
using Application.Dto;
using Application.Enums;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.CategoryFeat.UploadImages
{
    public record UploadProductImageCommand(int ProductId, IFormFile File) : IRequest<Result<ProductImageDTO>>;

    public class UploadProductImageCommandHandler : IRequestHandler<UploadProductImageCommand, Result<ProductImageDTO>>
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductImageRepository _productImageRepository;
        private readonly IFileServices _fileService;
        public UploadProductImageCommandHandler(IProductRepository productRepository, IProductImageRepository productImageRepository, IFileServices fileService)
        {
            _productRepository = productRepository;
            _productImageRepository = productImageRepository;
            _fileService = fileService;

        }
        public async Task<Result<ProductImageDTO>> Handle(UploadProductImageCommand request, CancellationToken cancellationToken)
        {
            var product = _productRepository.FindByIdAsync(request.ProductId);
            if (product == null)
            {
                return Result<ProductImageDTO>.Failure("Product id not found");
            }

            // Validate File
            if (request.File == null || request.File.Length == 0)
            {
                return Result<ProductImageDTO>.Failure("Invalid file. Please upload a valid image.");
            }

            // Save the file using the file service
            string fileUrl;
            try
            {
                fileUrl = await _fileService.SaveFileAsync(request.File, FileType.ProductImages);
            }
            catch (Exception ex)
            {
                return Result<ProductImageDTO>.Failure($"File upload failed: {ex.Message}");
            }

            // Create a new ProductImage entity
            var productImage = new ProductImage
            {
                ProductId = request.ProductId,
                ImageUrl = fileUrl
            };

            // Save the ProductImage to the database
            await _productImageRepository.AddAsync(productImage, cancellationToken);

            // Map to DTO and return success
            return Result<ProductImageDTO>.Success(productImage.ToDTO(), "Product image uploaded successfully.");
        }
    }
}


