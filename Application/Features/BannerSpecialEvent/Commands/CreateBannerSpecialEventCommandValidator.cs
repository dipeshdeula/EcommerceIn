using Application.Interfaces.Repositories;
using Domain.Enums.BannerEventSpecial;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.BannerSpecialEvent.Commands
{
    public class CreateBannerSpecialEventCommandValidator : AbstractValidator<CreateBannerSpecialEventCommand>
    {
        private readonly IBannerEventSpecialRepository _bannerEventRepository;

        public CreateBannerSpecialEventCommandValidator(IBannerEventSpecialRepository bannerEventRepository)
        {
            _bannerEventRepository = bannerEventRepository;

            // Event DTO validation
            
            RuleFor(x => x.EventDto.Name)
                    .NotEmpty().WithMessage("Event Name is required.")
                    .MaximumLength(100).WithMessage("Event Name cannot exceed 100 characters.");

            RuleFor(x => x.EventDto.Description)
                .NotEmpty().WithMessage("Event description is required.")
                .MaximumLength(1000).WithMessage("Event description cannot exceed 1000 characters.");

            RuleFor(x => x.EventDto.TagLine)
                .MaximumLength(200).WithMessage("Tag line cannot exceed 200 characters.");

            //Discount validation
            RuleFor(x => x.EventDto.DiscountValue)
                .GreaterThan(0).WithMessage("Discount value must be greater than 0.");

            RuleFor(x => x.EventDto.DiscountValue)
                .LessThanOrEqualTo(100)
                .When(x => x.EventDto.PromotionType == PromotionType.Percentage)
                .WithMessage("Percentage discount cannot exceed 100%");

            RuleFor(x => x.EventDto.MaxDiscountAmount)
                .GreaterThan(0)
                .When(x => x.EventDto.MaxDiscountAmount.HasValue)
                .WithMessage("Maximum discount amount must be greater than 0.");

            RuleFor(x => x.EventDto.MinOrderValue)
                .GreaterThan(0)
                .When(x => x.EventDto.MinOrderValue.HasValue)
                .WithMessage("Minimum order value must be greater than 0.");

            // Date validation
            RuleFor(x => x.EventDto.StartDate).LessThan(x => x.EventDto.StartDate)
                .WithMessage("Start date must be before end date");

            RuleFor(x => x.EventDto.EndDate).GreaterThan(x => x.EventDto.StartDate)
                .WithMessage("End date must be after start date");

            RuleFor(x => x.EventDto.StartDate)
               .GreaterThanOrEqualTo(DateTime.UtcNow.AddMinutes(-5))
               .WithMessage("Start date cannot be in the past.");

            RuleFor(x => x)
                .MustAsync(NotHaveConflictingEvents)
                .WithMessage("Event conflicts with existing high-priority events.");

            // Usage limits validation
            RuleFor(x => x.EventDto.MaxUsageCount)
                .GreaterThan(0)
                .When(x => x.EventDto.MaxUsageCount.HasValue)
                .WithMessage("Maximum usage count must be greater than 0.");

            RuleFor(x => x.EventDto.MaxUsagePerUser)
                .GreaterThan(0)
                .When(x => x.EventDto.MaxUsagePerUser.HasValue)
                .WithMessage("Maximum usage per user must be greater than 0.");

            RuleFor(x => x.EventDto.Priority)
                .GreaterThan(0)
                .When(x => x.EventDto.Priority.HasValue)
                .WithMessage("Priority must be greater than 0.");

            // Rules validation
            RuleForEach(x => x.Rules)
                .SetValidator(new EventRuleDtoValidator())
                .When(x => x.Rules != null && x.Rules.Any());

            // Products validation
            RuleFor(x => x.ProductIds)
                .Must(x => x == null || x.Count <= 1000)
                .WithMessage("Cannot associate more than 1000 products at once.");

          /*  // Banner images validation
            RuleFor(x => x.BannerImages)
                .Must(x => x == null || x.Count <= 10)
                .WithMessage("Cannot upload more than 10 banner images.");*/


        }
        private async Task<bool> NotHaveConflictingEvents(CreateBannerSpecialEventCommand command, CancellationToken cancellationToken)
        {
            var eventDto = command.EventDto;
            var priority = eventDto.Priority ?? 1;

            var conflictingEvents = await _bannerEventRepository.GetAllAsync(
                filter: e => e.IsActive &&
                            !e.IsDeleted &&
                            e.Priority >= priority &&
                            e.StartDate <= eventDto.EndDate &&
                            e.EndDate >= eventDto.StartDate
            );

            return !conflictingEvents.Any();
        }

    }
}
