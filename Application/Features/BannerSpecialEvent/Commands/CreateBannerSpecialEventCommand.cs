using Application.Common;
using Application.Dto.BannerEventSpecialDTOs;
using Application.Extension;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Entities.Common;
using Domain.Enums.BannerEventSpecial;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.BannerSpecialEvent.Commands
{
    public record CreateBannerSpecialEventCommand(
         CreateBannerSpecialEventDTO bannerSpecialDTO
     ) : IRequest<Result<BannerEventSpecialDTO>>;

    public class CreateBannerSpecialEventCommandHandler : IRequestHandler<CreateBannerSpecialEventCommand, Result<BannerEventSpecialDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateBannerSpecialEventCommandHandler> _logger;
        private readonly INepalTimeZoneService _nepalTimeZoneService;

        public CreateBannerSpecialEventCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<CreateBannerSpecialEventCommandHandler> logger,
            INepalTimeZoneService nepalTimeZoneService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _nepalTimeZoneService = nepalTimeZoneService;
        }

        public async Task<Result<BannerEventSpecialDTO>> Handle(CreateBannerSpecialEventCommand request, CancellationToken cancellationToken)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var inputDto = request.bannerSpecialDTO.EventDto;

                    // ✅ PARSE NEPAL TIME STRINGS TO DATETIME
                    if (!DateTime.TryParse(inputDto.StartDateNepal, out var startDateNepalParsed))
                        throw new ArgumentException($"Invalid start date format: {inputDto.StartDateNepal}");

                    if (!DateTime.TryParse(inputDto.EndDateNepal, out var endDateNepalParsed))
                        throw new ArgumentException($"Invalid end date format: {inputDto.EndDateNepal}");

                    // ✅ CONVERT NEPAL TIME TO UTC WITH PROPER DateTimeKind
                    var startDateUtc = _nepalTimeZoneService.ConvertFromNepalToUtc(startDateNepalParsed);
                    var endDateUtc = _nepalTimeZoneService.ConvertFromNepalToUtc(endDateNepalParsed);

                    // ✅ ENSURE UTC KIND FOR POSTGRESQL
                    startDateUtc = DateTime.SpecifyKind(startDateUtc, DateTimeKind.Utc);
                    endDateUtc = DateTime.SpecifyKind(endDateUtc, DateTimeKind.Utc);

                    // ✅ CREATE BANNER EVENT ENTITY USING MAPPING EXTENSION
                    var bannerEvent = inputDto.ToEntity(startDateUtc, endDateUtc);
                    var createdEvent = await _unitOfWork.BannerEventSpecials.AddAsync(bannerEvent, cancellationToken);

                    // ✅ CREATE EVENT RULES
                    if (request.bannerSpecialDTO.Rules?.Any() == true)
                    {
                        var ruleEntities = request.bannerSpecialDTO.Rules.Select(ruleDto => new EventRule
                        {
                            BannerEventId = createdEvent.Id,
                            Type = ruleDto.Type,
                            TargetValue = ruleDto.TargetValue ?? string.Empty,
                            Conditions = ruleDto.Conditions ?? string.Empty,
                            DiscountType = ruleDto.DiscountType,
                            DiscountValue = ruleDto.DiscountValue,
                            MaxDiscount = ruleDto.MaxDiscount,
                            MinOrderValue = ruleDto.MinOrderValue,
                            Priority = ruleDto.Priority
                        }).ToList();

                        await _unitOfWork.BulkInsertAsync(ruleEntities);
                    }

                    // ✅ ASSOCIATE PRODUCTS (FluentValidation already validated they exist)
                    if (request.bannerSpecialDTO.ProductIds?.Any() == true)
                    {
                        var productEntities = request.bannerSpecialDTO.ProductIds.Select(productId => new EventProduct
                        {
                            BannerEventId = createdEvent.Id,
                            ProductId = productId,
                            AddedAt = DateTime.UtcNow
                        }).ToList();

                        await _unitOfWork.BulkInsertAsync(productEntities);
                    }

                    // ✅ CONVERT TO DTO WITH NEPAL TIME DISPLAY
                    var resultDto = createdEvent.ToDTO(_nepalTimeZoneService);

                    _logger.LogInformation("Banner event created successfully: {EventId} - {EventName} | Nepal Time: {StartDateNepal} to {EndDateNepal}",
                        createdEvent.Id, createdEvent.Name, inputDto.StartDateNepal, inputDto.EndDateNepal);

                    return Result<BannerEventSpecialDTO>.Success(resultDto,
                        "Banner event created successfully with Nepal time zone support. Use activation command to make it live.");
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input data for banner event: {EventName}",
                    request.bannerSpecialDTO.EventDto.Name);
                return Result<BannerEventSpecialDTO>.Failure($"Invalid input: {ex.Message}");
            }
            catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx)
            {
                _logger.LogError(ex, "Database constraint violation: {ErrorCode} - {Message}",
                    pgEx.ErrorCode, pgEx.MessageText);

                var errorMessage = GetUserFriendlyErrorMessage(pgEx.ErrorCode.ToString(), pgEx.MessageText);
                return Result<BannerEventSpecialDTO>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create banner event: {EventName}",
                    request.bannerSpecialDTO.EventDto.Name);
                return Result<BannerEventSpecialDTO>.Failure($"Failed to create banner event: {ex.Message}");
            }
        }

        private static string GetUserFriendlyErrorMessage(string errorCode, string? defaultMessage)
        {
            return errorCode switch
            {
                "23503" => "Invalid product reference or foreign key constraint violation",
                "23505" => "Banner event with this name already exists",
                "23514" => "Check constraint violation - invalid data provided",
                "23P01" => "Exclusion constraint violation - conflicting data",
                "42P01" => "Database table does not exist",
                "42703" => "Database column does not exist",
                _ => $"Database error: {defaultMessage ?? "Unknown database error"}"
            };
        }
    }
}