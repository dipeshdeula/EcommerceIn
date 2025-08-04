using Application.Common;
using Application.Dto.PaymentMethodDTOs;
using Application.Enums;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.PaymentMethodFeat.Commands
{
    public record CreatePaymentMethodCommand(
        string Name,
        PaymentMethodType Type,
        IFormFile File
        ) : IRequest<Result<PaymentMethodDTO>>;

    public class CreatePaymentMethodCommandHandler : IRequestHandler<CreatePaymentMethodCommand, Result<PaymentMethodDTO>>
    {
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        private readonly IFileServices _fileService;
        public CreatePaymentMethodCommandHandler(IPaymentMethodRepository paymentMethodRepository,IFileServices fileService)
        {
            _paymentMethodRepository = paymentMethodRepository;
            _fileService = fileService;
            
        }
        public async Task<Result<PaymentMethodDTO>> Handle(CreatePaymentMethodCommand request, CancellationToken cancellationToken)
        {
            string fileUrl = null;

            if (request.File != null && request.File.Length > 0)
            {
                try
                {
                    fileUrl = await _fileService.SaveFileAsync(request.File, FileType.PaymentMethodImages);
                }
                catch (Exception ex)
                {
                    return Result<PaymentMethodDTO>.Failure($"Image Upload failed: {ex.Message}");
                }

            }

            var paymentMethod = new PaymentMethod
            {
                ProviderName = request.Name,
                Type = request.Type,
                Logo = fileUrl
            };

            var createPaymentMethod = await _paymentMethodRepository.AddAsync( paymentMethod );
            await _paymentMethodRepository.SaveChangesAsync(cancellationToken);
            if (createPaymentMethod == null)
                return Result<PaymentMethodDTO>.Failure("Failed to create payment method");

            return Result<PaymentMethodDTO>.Success(createPaymentMethod.ToDTO(), "Payment method created successfully");
        }
    }

}
