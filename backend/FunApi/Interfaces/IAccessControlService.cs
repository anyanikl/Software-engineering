namespace FunApi.Interfaces
{
    public interface IAccessControlService
    {
        Task EnsureAnyRoleAsync(int userId, params string[] roles);
        Task<bool> HasAnyRoleAsync(int userId, params string[] roles);
    }
}
