using Application.Common;
using Application.Dto;
using Application.Enums;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;

public record UploadProductImagesCommand(int ProductId, IFormFileCollection Files) : IRequest<Result<IEnumerable<ProductImageDTO>>>;

public class UploadProductImagesCommandHandler : IRequestHandler<UploadProductImagesCommand, Result<IEnumerable<ProductImageDTO>>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductImageRepository _productImageRepository;
    private readonly IFileServices _fileService;

    public UploadProductImagesCommandHandler(IProductRepository productRepository, IProductImageRepository productImageRepository, IFileServices fileService)
    {
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _fileService = fileService;
    }

    public async Task<Result<IEnumerable<ProductImageDTO>>> Handle(UploadProductImagesCommand request, CancellationToken cancellationToken)
    {
        // Validate ProductId
        var product = await _productRepository.FindByIdAsync(request.ProductId);
        if (product == null)
        {
            return Result<IEnumerable<ProductImageDTO>>.Failure("Product not found.");
        }

        // Validate Files
        if (request.Files == null || request.Files.Count == 0)
        {
            return Result<IEnumerable<ProductImageDTO>>.Failure("No files uploaded. Please upload at least one image.");
        }

        var productImages = new List<ProductImage>();
        var productImageDTOs = new List<ProductImageDTO>();

        foreach (var file in request.Files)
        {
            // Save each file using the file service
            string fileUrl;
            try
            {
                fileUrl = await _fileService.SaveFileAsync(file, FileType.ProductImages);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<ProductImageDTO>>.Failure($"File upload failed: {ex.Message}");
            }

            // Create a new ProductImage entity
            var productImage = new ProductImage
            {
                ProductId = request.ProductId,
                ImageUrl = fileUrl
            };

            productImages.Add(productImage);
            productImageDTOs.Add(productImage.ToDTO());
        }

        // Save all ProductImages to the database
        await _productImageRepository.AddRangeAsync(productImages, cancellationToken);
        await _productImageRepository.SaveChangesAsync(cancellationToken);

        // Return success with the list of uploaded images
        return Result<IEnumerable<ProductImageDTO>>.Success(productImageDTOs, "Product images uploaded successfully.");
    }
}
