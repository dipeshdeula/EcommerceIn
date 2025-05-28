using Application.Common;
using Application.Dto;
using Application.Enums;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.PaymentMethodFeat.Commands
{
    public record UpdatePaymentMethodCommand (
        int Id,
        string? Name,
        PaymentMethodType? Type,
        IFormFile? File
        ) : IRequest<Result<PaymentMethodDTO>>;

    public class UpdatePaymentMethodCommandHandler : IRequestHandler<UpdatePaymentMethodCommand, Result<PaymentMethodDTO>>
    {
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        private readonly IFileServices _fileServices;

        public UpdatePaymentMethodCommandHandler(IPaymentMethodRepository paymentMethodRepository,IFileServices fileService)
        {
            _paymentMethodRepository = paymentMethodRepository;
            _fileServices = fileService;
        }
        public async Task<Result<PaymentMethodDTO>> Handle(UpdatePaymentMethodCommand request, CancellationToken cancellationToken)
        {
            var payment = await _paymentMethodRepository.FindByIdAsync(request.Id);
            if (payment == null)
                return Result<PaymentMethodDTO>.Failure("Payment Id is not found");

            payment.Name = request.Name ?? payment.Name;
            payment.Type = request.Type ?? payment.Type;

            if (request.File != null)
            {
                try
                {
                    payment.Logo = await _fileServices.UpdateFileAsync(payment.Logo, request.File, FileType.PaymentMethodImages);

                }
                catch (Exception ex)
                {
                    return Result<PaymentMethodDTO>.Failure("Image update failed");

                }
            }
            await _paymentMethodRepository.UpdateAsync(payment, cancellationToken);
            await _paymentMethodRepository.SaveChangesAsync(cancellationToken);

            var paymentDTO = new PaymentMethodDTO
            {
                Id = payment.Id,
                Name = payment.Name,
                Type = payment.Type,
                Logo = payment.Logo
            };

            return Result<PaymentMethodDTO>.Success(paymentDTO,"Payment Updated Successfully");



        }
    }

}
