using FunDto.Models.Contracts.Advertisements;

namespace FunApi.Interfaces
{
    public interface IModerationService
    {
        Task<List<ModerationAdvertisementDto>> GetPendingAsync(int moderatorId);
        Task ApproveAsync(int moderatorId, int advertisementId, string? comment);
        Task RejectAsync(int moderatorId, int advertisementId, string comment);
        Task SendForRevisionAsync(int moderatorId, int advertisementId, string comment);
    }
}
