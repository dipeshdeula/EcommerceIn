namespace Application.Dto.CartItemDTOs
{
    public class CartValidationResultDTO
    {
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool CanProceedToCheckout => IsValid && !Messages.Any();
    }
}