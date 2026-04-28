using FunDto.Models.Contracts.Config;

namespace FunApi.Interfaces
{
    public interface IAppConfigService
    {
        Task<AppConfigDto> GetAsync(CancellationToken cancellationToken = default);
    }
}
