using FunDto.Models.Contracts.Cart;

namespace FunApi.Interfaces
{
    public interface ICartService
    {
        Task<CartDto> GetCartAsync(int userId);
        Task AddItemAsync(int userId, int advertisementId);
        Task RemoveItemAsync(int userId, int advertisementId);
        Task ClearAsync(int userId);
    }
}
