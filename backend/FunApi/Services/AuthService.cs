using FunApi.Interfaces;
using FunDto.Models.Contracts.Auth;

namespace FunApi.Services
{
    public class AuthService : IAuthService
    {
        public Task ConfirmEmailAsync(string token)
        {
            throw new NotImplementedException();
        }

        public Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
        {
            throw new NotImplementedException();
        }

        public Task LogoutAsync()
        {
            throw new NotImplementedException();
        }

        public Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
        {
            throw new NotImplementedException();
        }

        public Task RequestPasswordResetAsync(string email)
        {
            throw new NotImplementedException();
        }

        public Task ResetPasswordAsync(ResetPasswordRequestDto dto)
        {
            throw new NotImplementedException();
        }
    }
}
