using FunDto.Models.Contracts.Advertisements;

namespace FunApi.Interfaces
{
    public interface IAdvertisementService
    {
        Task<AdvertisementDto> GetByIdAsync(int advertisementId, int? viewerId = null);
        Task<List<AdvertisementCardDto>> GetAllAsync(AdvertisementFilterDto filter);
        Task<List<AdvertisementCardDto>> SearchAsync(AdvertisementFilterDto filter);

        Task<AdvertisementDto> CreateAsync(int sellerId, CreateAdvertisementDto dto);
        Task<AdvertisementDto> UpdateAsync(int sellerId, int advertisementId, UpdateAdvertisementDto dto);
        Task DeleteAsync(int sellerId, int advertisementId);
        Task ArchiveAsync(int sellerId, int advertisementId);
        Task RestoreAsync(int sellerId, int advertisementId);

        Task<List<MyAdvertisementDto>> GetMyAdvertisementsAsync(int sellerId, string? status = null);
        Task<List<string>> AddImagesAsync(int sellerId, int advertisementId, List<IFormFile> files);
        Task DeleteImagesAsync(int sellerId, int advertisementId);
        Task DeleteImageAsync(int sellerId, int advertisementId, int imageId);
    }
}
