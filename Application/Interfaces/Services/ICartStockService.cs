using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface ICartStockService
    {
        Task<StockReservationResult> TryReserveStockAsync(int productId, int quantity, int userId, CancellationToken cancellationToken = default);
        Task<bool> ReleaseStockAsync(int productId, int quantity, CancellationToken cancellationToken = default);
        Task<bool> ConfirmStockAsync(int productId, int quantity, CancellationToken cancellationToken = default);
        Task<bool> UpdateReservationAsync(int productId, int oldQuantity, int newQuantity, CancellationToken cancellationToken = default);
    }
    public class StockReservationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int AvailableStock { get; set; }
        public string ReservationToken { get; set; } = string.Empty;

        public static StockReservationResult Succeeded(string token = "") =>
            new() { Success = true, ReservationToken = token };

        public static StockReservationResult Failed(string error, int available = 0) =>
            new() { Success = false, ErrorMessage = error, AvailableStock = available };
    }
}
