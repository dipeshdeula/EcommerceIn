using Application.Common;
using Application.Dto.BannerEventSpecialDTOs;
using Application.Enums;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Entities.Common;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.BannerSpecialEvent.Commands
{
    public record CreateBannerSpecialEventCommand
        (
        AddBannerEventSpecialDTO EventDto,
        List<AddEventRuleDTO>? Rules = null,
        List<int>? ProductIds = null
        ) : IRequest<Result<BannerEventSpecialDTO>>;

    public class CreateBannerSpecialEventCommandHandler : IRequestHandler<CreateBannerSpecialEventCommand, Result<BannerEventSpecialDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public CreateBannerSpecialEventCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<Result<BannerEventSpecialDTO>> Handle(CreateBannerSpecialEventCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var bannerEvent = new BannerEventSpecial
                {
                    Name = request.EventDto.Name,
                    Description = request.EventDto.Description,
                    TagLine = request.EventDto.TagLine,
                    EventType = request.EventDto.EventType,
                    PromotionType = request.EventDto.PromotionType,
                    DiscountValue = request.EventDto.DiscountValue,
                    MaxDiscountAmount = request.EventDto.MaxDiscountAmount,
                    MinOrderValue = request.EventDto.MinOrderValue,
                    StartDate = DateTime.SpecifyKind(request.EventDto.StartDate, DateTimeKind.Utc),
                    EndDate = DateTime.SpecifyKind(request.EventDto.EndDate, DateTimeKind.Utc),
                    ActiveTimeSlot = request.EventDto.ActiveTimeSlot,
                    MaxUsageCount = request.EventDto.MaxUsageCount ?? int.MaxValue,
                    MaxUsagePerUser = request.EventDto.MaxUsagePerUser ?? int.MaxValue,
                    Priority = request.EventDto.Priority ?? 1,
                    IsActive = false,
                    Status = Domain.Enums.BannerEventSpecial.EventStatus.Draft
                };

                var createdEvent = await _unitOfWork.BannerEventSpecials.AddAsync(bannerEvent);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Create event rules
                if (request.Rules?.Any() == true)
                {
                    var ruleEntities = request.Rules.Select(ruleDto => new EventRule
                    {
                        BannerEventId = createdEvent.Id,
                        Type = ruleDto.Type,
                        TargetValue = ruleDto.TargetValue,
                        Conditions = ruleDto.Conditions,
                        DiscountType = ruleDto.DiscountType,
                        DiscountValue = ruleDto.DiscountValue,
                        MaxDiscount = ruleDto.MaxDiscount,
                        MinOrderValue = ruleDto.MinOrderValue,
                        Priority = ruleDto.Priority,
                    }).ToList();

                    await _unitOfWork.BulkInsertAsync(ruleEntities);
                }

                // Associate specific products
                if (request.ProductIds?.Any() == true)
                {
                    var productEntities = request.ProductIds.Select(productId => new EventProduct
                    {
                        BannerEventId = createdEvent.Id,
                        ProductId = productId
                    }).ToList();
                    await _unitOfWork.BulkInsertAsync(productEntities);
                }

                    return Result<BannerEventSpecialDTO>.Success(createdEvent.ToDTO(),
                        "Banner event created successfully. Use activation command to make it live.");
                }
            
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<BannerEventSpecialDTO>.Failure($"Failed to create Event:{ex.Message}");
                
            }
        }
    }

}
