namespace Application.Dto.BannerEventSpecialDTOs
{
    public class CreateBannerSpecialEventDTO
    {
        public AddBannerEventSpecialDTO EventDto { get; set; }
        public List<AddEventRuleDTO>? Rules { get; set; } = null;
        public List<int>? ProductIds { get; set; } = null;
    }


}
