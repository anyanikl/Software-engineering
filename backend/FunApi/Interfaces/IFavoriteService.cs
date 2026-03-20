using FunDto.Models.Contracts.Advertisements;

namespace FunApi.Interfaces
{
    public interface IFavoriteService
    {
        Task<List<AdvertisementCardDto>> GetAllAsync(int userId);
        Task AddAsync(int userId, int advertisementId);
        Task RemoveAsync(int userId, int advertisementId);
        Task<bool> ExistsAsync(int userId, int advertisementId);
    }
}
