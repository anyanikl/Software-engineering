using FunDto.Models.Contracts.Advertisements;

namespace FunApi.Interfaces
{
    public interface IAdvertisementService
    {
        Task<AdvertisementDto> GetByIdAsync(int advertisementId);
        Task<List<AdvertisementCardDto>> GetAllAsync(AdvertisementFilterDto filter);
        Task<List<AdvertisementCardDto>> SearchAsync(AdvertisementFilterDto filter);

        Task<AdvertisementDto> CreateAsync(int sellerId, CreateAdvertisementDto dto);
        Task<AdvertisementDto> UpdateAsync(int sellerId, int advertisementId, UpdateAdvertisementDto dto);
        Task DeleteAsync(int sellerId, int advertisementId);
        Task ArchiveAsync(int sellerId, int advertisementId);

        Task<List<MyAdvertisementDto>> GetMyAdvertisementsAsync(int sellerId, string? status = null);
        Task AddImagesAsync(int sellerId, int advertisementId, List<IFormFile> files);
        Task DeleteImageAsync(int sellerId, int advertisementId, int imageId);
    }
}
