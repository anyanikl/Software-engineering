using FunDto.Models.Contracts.Admin;

namespace FunApi.Interfaces
{
    public interface IAdminService
    {
        Task<List<AdminStatsDto>> GetUsersAsync(int adminId, UserAdminFilterDto filter);
        Task BlockUserAsync(int adminId, int userId);
        Task UnblockUserAsync(int adminId, int userId);

        Task<UserAdminDto> GetStatsAsync(int adminId);
        Task<string> ExportUsersCsvAsync(int adminId);
        Task<string> ExportUsersJsonAsync(int adminId);
    }
}
