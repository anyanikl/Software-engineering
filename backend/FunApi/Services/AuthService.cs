using FunApi.Interfaces;
using FunApi.Models;
using FunApi.Models.Users;
using FunDto.Models.Contracts.Auth;
using FunDto.Models.Internal.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FunApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly FunDBcontext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthService(FunDBcontext context, ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
            _passwordHasher = new PasswordHasher<User>();
        }

        public Task ConfirmEmailAsync(string token)
        {
            throw new NotImplementedException();
        }

        public async Task<AuthUserInternalDto?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

            return user is null ? null : MapUser(user);
        }

        public async Task<AuthResultInternalDto> LoginAsync(LoginInternalDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
                var user = await _context.Users
                    .Include(x => x.Role)
                    .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);

                if (user is null)
                {
                    return AuthResultInternalDto.Failure("Пользователь не найден");
                }

                if (user.IsBlocked)
                {
                    return AuthResultInternalDto.Failure("Аккаунт заблокирован");
                }

                if (!user.IsEmailConfirmed)
                {
                    return AuthResultInternalDto.Failure("Подтвердите email перед входом");
                }

                var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
                if (verificationResult == PasswordVerificationResult.Failed)
                {
                    return AuthResultInternalDto.Failure("Неверный пароль");
                }

                if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);
                }

                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                return AuthResultInternalDto.Success(MapUser(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при авторизации");
                return AuthResultInternalDto.Failure("Внутренняя ошибка сервера");
            }
        }

        public Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<AuthResultInternalDto> RegisterAsync(RegisterInternalDto dto, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AuthResultInternalDto.Failure("Регистрация пока не реализована"));
        }

        public Task RequestPasswordResetAsync(string email)
        {
            throw new NotImplementedException();
        }

        public Task ResetPasswordAsync(ResetPasswordRequestDto dto)
        {
            throw new NotImplementedException();
        }

        private static AuthUserInternalDto MapUser(User user)
        {
            return new AuthUserInternalDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role?.Name ?? string.Empty
            };
        }
    }
}
