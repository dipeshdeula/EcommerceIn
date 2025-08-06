namespace Application.Features.DashboardFeat.Queries
{
    internal class PaymentMethodStatsDTO
    {
        public string? PaymentMethod { get; set; }
        public object TotalAmount { get; set; }
        public int TransactionCount { get; set; }
    }
}