using FunDto.Models.Contracts.Auth;
using FunDto.Models.Internal.Auth;

namespace FunApi.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResultInternalDto> LoginAsync(LoginInternalDto dto, CancellationToken cancellationToken = default);
        Task<AuthUserInternalDto?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);
        Task<AuthResultInternalDto> RegisterAsync(RegisterInternalDto dto, CancellationToken cancellationToken = default);
        Task LogoutAsync(CancellationToken cancellationToken = default);
        Task RequestPasswordResetAsync(string email);
        Task ResetPasswordAsync(ResetPasswordRequestDto dto);
        Task ConfirmEmailAsync(string token);
    }
}
