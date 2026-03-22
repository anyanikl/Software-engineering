using FunDto.Models.Internal.Auth;

namespace FunApi.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResultInternalDto> LoginAsync(LoginInternalDto dto, CancellationToken cancellationToken = default);
        Task<AuthUserInternalDto?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);
        Task<AuthResultInternalDto> RegisterAsync(RegisterInternalDto dto, CancellationToken cancellationToken = default);
        Task LogoutAsync(CancellationToken cancellationToken = default);
        Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);
        Task ResetPasswordAsync(string token, string newPassword, string confirmPassword, CancellationToken cancellationToken = default);
        Task ConfirmEmailAsync(string token, CancellationToken cancellationToken = default);
    }
}
