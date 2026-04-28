using FunDto.Models.Contracts.Orders;

namespace FunApi.Interfaces
{
    public interface IOrderService
    {
        Task<OrderDto> CreateFromCartAsync(int buyerId);
        Task<OrderDto> CreateSingleAsync(int buyerId, int advertisementId);
        Task<List<OrderDto>> GetBuyerOrdersAsync(int buyerId);
        Task<List<OrderDto>> GetSellerOrdersAsync(int sellerId);

        Task CompleteAsync(int userId, int orderId);
        Task CancelAsync(int userId, int orderId);
        Task<OrderDto> GetByIdAsync(int userId, int orderId);
    }
}
