/*using Application.Common;
using Application.Dto;
using Application.Dto.PaymentDTOs;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace Application.Features.PaymentRequestFeat.Commands
{
    public record CreatePaymentRequestCommand(

        AddPamentRequestDTO addpaymentRequest
        ) : IRequest<Result<PaymentRequestDTO>>;

    public class CreatePaymentRequestCommandHandler : IRequestHandler<CreatePaymentRequestCommand, Result<PaymentRequestDTO>>
    {
        private readonly IPaymentRequestRepository _paymentRequestRepository;
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        private readonly IUserRepository _userRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly PaymentContextDto _context;
        public CreatePaymentRequestCommandHandler(
            IPaymentRequestRepository prr,
            IPaymentMethodRepository pmr,
            IUserRepository ur,
            IOrderRepository or,
            IHttpClientFactory hf,
            IConfiguration cf,
            PaymentContextDto context
            )
        {
            _paymentRequestRepository = prr;
            _paymentMethodRepository = pmr;
            _userRepository = ur;
            _orderRepository = or;
            _httpClientFactory = hf;
            _configuration = cf;
            _context = context;

        }

        public async Task<Result<PaymentRequestDTO>> Handle(CreatePaymentRequestCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.FindByIdAsync(request.addpaymentRequest.UserId);
            var order = await _orderRepository.FindByIdAsync(request.addpaymentRequest.OrderId);
            var paymentMethod = await _paymentMethodRepository.FindByIdAsync(request.addpaymentRequest.PaymentMethodId);

            if (user == null || order == null || paymentMethod == null)
            {
                return Result<PaymentRequestDTO>.Failure($"Id not found, user:{user.Id},order:{order.Id},paymentMethod:{paymentMethod.Id}");
            }

            if (user.Id != order.UserId)
            {
                return Result<PaymentRequestDTO>.Failure($"Id mismatch , user.id :{user.Id} and order.UserId:{order.UserId} must be same");
            }

            var pr = new PaymentRequest
            {
                CreatedAt = DateTime.UtcNow,
                UserId = user.Id,
                OrderId = request.addpaymentRequest.OrderId,
                PaymentMethodId = request.addpaymentRequest.PaymentMethodId,
                Description = request.addpaymentRequest.Description,
                PaymentStatus = "Pending",
                PaymentAmount = order.TotalAmount,

            };

            await _paymentRequestRepository.AddAsync(pr, cancellationToken);
            await _paymentMethodRepository.SaveChangesAsync(cancellationToken);

            if (pr.Id == 0)
            {
                throw new Exception("Failed to save PaymentRequest to database");
            }


            PaymentRequestDTO paymentResponse;


            switch (pr.PaymentMethodId)
            {
                case 1:
                    paymentResponse = await InitiateEsewaPayment(pr, cancellationToken);
                    break;
                case 2:
                    paymentResponse = await InitiateKhaltiPayment(_context, cancellationToken);
                    break;
                //case 3:
                //    paymentResponse = await InitiateCODPayment(pr, cancellationToken);
                //    break;
                default:
                    return Result<PaymentRequestDTO>.Failure("Unsupported payment method.");


            }
            pr.UpdatedAt = DateTime.UtcNow;
            await _paymentRequestRepository.UpdateAsync(pr, cancellationToken);
            return Result<PaymentRequestDTO>.Success(paymentResponse,"Payment processing..");
        }
           


            private async Task<PaymentRequestDTO> InitiateKhaltiPayment(PaymentContextDto paymentContext, CancellationToken cancellationToken)
        {

            var client = _httpClientFactory.CreateClient();
            var khaltiKey = _configuration["PaymentGateways:Khalti:SecretKey"];
            var khaltiBaseUrl = _configuration["PaymentGateways:Khalti:BaseUrl"];

            if (string.IsNullOrEmpty(khaltiKey) || string.IsNullOrEmpty(khaltiBaseUrl))
            {
                throw new Exception("Khalti configuration (SecretKey or BaseUrl) is missing");
            }

            var payload = new
            {
                return_url = "http://localhost:5173/payment/success",
                website_url = "http://localhost:5225",
                amount = paymentContext.PaymentRequest.PaymentAmount,
                purchase_order_id = $"Order_{paymentContext.PaymentRequest.OrderId}",
                purchase_order_name = paymentContext.PaymentRequest.Description,
                customer_info = new { name = paymentContext.User.Name, email = paymentContext.User.Email, phone = paymentContext.User.Contact }
            };

            client.DefaultRequestHeaders.Add("Authorization", $"Key {khaltiKey}");
            var response = await client.PostAsJsonAsync($"{khaltiBaseUrl}epayment/initiate/", payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Log raw response for debugging
            var rawResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Khalti Raw Response: {rawResponse}");

            var result = await response.Content.ReadFromJsonAsync<KhaltiResponseDto>(cancellationToken: cancellationToken);
            if (result == null || string.IsNullOrEmpty(result.Pidx))
            {
                throw new Exception($"Khalti failed to return a valid response. Raw response: {rawResponse}");
            }

            paymentContext.PaymentRequest.KhaltiPidx = result.Pidx;

            if (string.IsNullOrEmpty(result.PaymentUrl))
            {
                throw new Exception($"Khalti returned a null or empty payment_url. Pidx: {result.Pidx}, Raw response: {rawResponse}");
            }

            return new PaymentRequestDTO
            {
                Id = paymentContext.PaymentRequest.Id,
                UserId = paymentContext.User.Id,
                OrderId = paymentContext.PaymentRequest.OrderId,
                PaymentMethodId = paymentContext.PaymentRequest.PaymentMethodId,
                PaymentAmount = paymentContext.PaymentRequest.PaymentAmount,
                Description = paymentContext.PaymentRequest.Description,
                KhaltiPidx = paymentContext.PaymentRequest.KhaltiPidx,
                CreatedAt=paymentContext.PaymentRequest.CreatedAt,
                PaymentUrl = result.PaymentUrl,
                
            };
        }

        private async Task<PaymentRequestDTO> InitiateEsewaPayment(PaymentRequest paymentRequest, CancellationToken cancellationToken)
        {
            // Create a new HttpClient instance using IHttpClientFactory
            var client = _httpClientFactory.CreateClient();

            // Retrieve eSewa configuration values
            var esewaMerchantId = _configuration["PaymentGateways:Esewa:MerchantId"];
            var esewaBaseUrl = _configuration["PaymentGateways:Esewa:BaseUrl"];
            var esewaSecret = _configuration["PaymentGateways:Esewa:SecretKey"];

            if (string.IsNullOrEmpty(esewaMerchantId) || string.IsNullOrEmpty(esewaBaseUrl) || string.IsNullOrEmpty(esewaSecret))
            {
                throw new Exception("eSewa configuration (MerchantId, BaseUrl, or SecretKey) is missing");
            }

            // Generate a unique transaction UUID
            var transactionUuid = $"TXN_{paymentRequest.Id+1}_{DateTime.UtcNow.Ticks}";
            var totalAmount = paymentRequest.PaymentAmount.ToString("F2");
            var signedFieldNames = "total_amount,transaction_uuid,product_code";
            var signatureString = $"total_amount={totalAmount},transaction_uuid={transactionUuid},product_code={esewaMerchantId}";

            // Generate HMAC signature
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(esewaSecret));
            var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
            var signature = Convert.ToBase64String(signatureBytes);

            // Prepare the form data
            var formData = new List<KeyValuePair<string, string>>
    {
        new("amount", paymentRequest.PaymentAmount.ToString("F2")),
        new("tax_amount", "0"),
        new("total_amount", totalAmount),
        new("transaction_uuid", transactionUuid),
        new("product_code", esewaMerchantId),
        new("product_service_charge", "0"),
        new("product_delivery_charge", "0"),
        new("success_url", "http://localhost:5173/payment/success"),
        new("failure_url", "http://localhost:5173/payment/failure"),
        new("signed_field_names", signedFieldNames),
        new("signature", signature)
    };

            try
            {
                // Log the request payload for debugging
                Console.WriteLine("eSewa Request Payload:");
                foreach (var item in formData)
                {
                    Console.WriteLine($"{item.Key}: {item.Value}");
                }

                // Send the HTTP POST request
                var content = new FormUrlEncodedContent(formData);
                var response = await client.PostAsync($"{esewaBaseUrl}form", content, cancellationToken);

                // Log the raw response for debugging
                var rawResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"eSewa Raw Response: {rawResponse}");

                // Ensure the response status code is successful
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"eSewa API returned an error. Status Code: {(int)response.StatusCode}, Response: {rawResponse}");
                }

                // Extract the redirect URL from the response
                var redirectUrl = response.RequestMessage?.RequestUri?.ToString();
                if (string.IsNullOrEmpty(redirectUrl))
                {
                    throw new Exception("eSewa failed to return a valid redirect URL");
                }

                // Update the payment request with the transaction ID
                paymentRequest.EsewaTransactionId = transactionUuid;

                // Return the payment response
                return new PaymentRequestDTO
                {
                    Id = paymentRequest.Id,
                    UserId = paymentRequest.UserId,
                    OrderId = paymentRequest.PaymentMethodId,
                    PaymentAmount = paymentRequest.PaymentAmount,
                    Description = paymentRequest.Description,
                    EsewaTransactionId = paymentRequest.EsewaTransactionId,
                    CreatedAt = paymentRequest.CreatedAt,
                    PaymentUrl = redirectUrl,
                };
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Failed to connect to eSewa: {ex.Message} (Status Code: {(int)ex.StatusCode})", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in InitiateEsewaPayment: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }


    }
}


*/

using Application.Common;
using Application.Dto.PaymentDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.PaymentRequestFeat.Commands
{
    public record CreatePaymentRequestCommand(
        AddPamentRequestDTO addpaymentRequest
    ) : IRequest<Result<PaymentRequestDTO>>;

    public class CreatePaymentRequestCommandHandler : IRequestHandler<CreatePaymentRequestCommand, Result<PaymentRequestDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentGatewayService _paymentGatewayService;

        public CreatePaymentRequestCommandHandler(
            IUnitOfWork unitOfWork,
            IPaymentGatewayService paymentGatewayService)
        {
            _unitOfWork = unitOfWork;
            _paymentGatewayService = paymentGatewayService;
        }

        public async Task<Result<PaymentRequestDTO>> Handle(CreatePaymentRequestCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate entities
                var user = await _unitOfWork.Users.GetByIdAsync(request.addpaymentRequest.UserId, cancellationToken);
                var order = await _unitOfWork.Orders.GetByIdAsync(request.addpaymentRequest.OrderId, cancellationToken);
                var paymentMethod = await _unitOfWork.PaymentMethods.GetByIdAsync(request.addpaymentRequest.PaymentMethodId, cancellationToken);

                if (user == null)
                {
                    return Result<PaymentRequestDTO>.Failure($"User with ID {request.addpaymentRequest.UserId} not found");
                }

                if (order == null)
                {
                    return Result<PaymentRequestDTO>.Failure($"Order with ID {request.addpaymentRequest.OrderId} not found");
                }

                if (paymentMethod == null)
                {
                    return Result<PaymentRequestDTO>.Failure($"Payment method with ID {request.addpaymentRequest.PaymentMethodId} not found");
                }

                // Validate ownership
                if (user.Id != order.UserId)
                {
                    return Result<PaymentRequestDTO>.Failure($"User ID mismatch: user.Id={user.Id}, order.UserId={order.UserId}");
                }

                // Validate order status
                if (order.Status == "Cancelled")
                {
                    return Result<PaymentRequestDTO>.Failure("Cannot create payment for cancelled order");
                }

                // Check for existing pending payment
                var existingPayment = await _unitOfWork.PaymentRequests.GetAsync(
                    predicate: pr => pr.OrderId == order.Id &&
                                   (pr.PaymentStatus == "Pending" || pr.PaymentStatus == "Initiated"),
                    cancellationToken: cancellationToken);

                if (existingPayment != null)
                {
                    return Result<PaymentRequestDTO>.Failure("A pending payment already exists for this order");
                }

                // Create payment request
                var paymentRequest = new PaymentRequest
                {
                    CreatedAt = DateTime.UtcNow,
                    UserId = user.Id,
                    OrderId = order.Id,
                    PaymentMethodId = request.addpaymentRequest.PaymentMethodId,
                    Description = request.addpaymentRequest.Description ?? $"Payment for Order #{order.Id}",
                    PaymentStatus = "Pending",
                    PaymentAmount = order.TotalAmount,
                    Currency = "NPR"
                };

                await _unitOfWork.PaymentRequests.AddAsync(paymentRequest, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (paymentRequest.Id == 0)
                {
                    return Result<PaymentRequestDTO>.Failure("Failed to create payment request");
                }

                //  Initiate payment with unified gateway service
                var initiationResult = await _paymentGatewayService.InitiatePaymentAsync(paymentRequest, cancellationToken);

                if (!initiationResult.Succeeded)
                {
                    // Update payment status to failed
                    paymentRequest.PaymentStatus = "Failed";
                    paymentRequest.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.PaymentRequests.UpdateAsync(paymentRequest, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return Result<PaymentRequestDTO>.Failure($"Payment initiation failed: {initiationResult.Message}");
                }

                // Build response DTO
                var responseDto = new PaymentRequestDTO
                {
                    Id = paymentRequest.Id,
                    UserId = paymentRequest.UserId,
                    OrderId = paymentRequest.OrderId,
                    PaymentMethodId = paymentRequest.PaymentMethodId,
                    PaymentAmount = paymentRequest.PaymentAmount,
                    Currency = paymentRequest.Currency,
                    Description = paymentRequest.Description,
                    PaymentStatus = paymentRequest.PaymentStatus,
                    CreatedAt = paymentRequest.CreatedAt,
                    UpdatedAt = paymentRequest.UpdatedAt,

                    //  Provider-specific fields
                    PaymentUrl = initiationResult.Data.PaymentUrl,
                    EsewaTransactionId = paymentRequest.EsewaTransactionId,
                    KhaltiPidx = paymentRequest.KhaltiPidx,

                    // Additional metadata
                    ExpiresAt = initiationResult.Data.ExpiresAt,
                    Instructions = initiationResult.Data.Instructions,
                    RequiresRedirect = initiationResult.Data.RequiresRedirect,
                    Metadata = initiationResult.Data.Metadata
                };

                return Result<PaymentRequestDTO>.Success(responseDto, "Payment request created and initiated successfully");
            }
            catch (Exception ex)
            {
                return Result<PaymentRequestDTO>.Failure($"An error occurred while creating payment request: {ex.Message}");
            }
        }
    }
}