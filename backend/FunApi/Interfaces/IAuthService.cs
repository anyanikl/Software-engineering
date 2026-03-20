using FunDto.Models.Contracts.Auth;

namespace FunApi.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);
        Task LogoutAsync();
        Task RequestPasswordResetAsync(string email);
        Task ResetPasswordAsync(ResetPasswordRequestDto dto);
        Task ConfirmEmailAsync(string token);
    }
}
