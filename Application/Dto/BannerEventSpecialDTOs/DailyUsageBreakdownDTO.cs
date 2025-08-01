namespace Application.Dto.BannerEventSpecialDTOs
{
    public class DailyUsageBreakdownDTO
    {
        public DateTime Date { get; set; }
        public int UsageCount { get; set; }
        public decimal TotalDiscount { get; set; }
        public int UniqueUsers { get; set; }
    }
}