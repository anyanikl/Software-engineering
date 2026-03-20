using FunDto.Models.Contracts.Admin;

namespace FunApi.Interfaces
{
    public interface IAdminService
    {
        Task<UserAdminDto> GetUsersAsync(UserAdminFilterDto filter);
        Task BlockUserAsync(int adminId, int userId);
        Task UnblockUserAsync(int adminId, int userId);

        Task<AdminStatsDto> GetStatsAsync();
        Task<string> ExportUsersCsvAsync();
        Task<string> ExportUsersJsonAsync();
    }
}
