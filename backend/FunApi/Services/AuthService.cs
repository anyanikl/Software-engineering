using FunApi.Interfaces;
using FunApi.Models;
using FunApi.Models.Auth;
using FunApi.Models.Carts;
using FunApi.Models.Users;
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

        public Task ConfirmEmailAsync(string token, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
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
                    return AuthResultInternalDto.Failure("User not found");
                }

                if (user.IsBlocked)
                {
                    return AuthResultInternalDto.Failure("Account is blocked");
                }

                if (!user.IsEmailConfirmed)
                {
                    return AuthResultInternalDto.Failure("Email is not confirmed");
                }

                var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
                if (verificationResult == PasswordVerificationResult.Failed)
                {
                    return AuthResultInternalDto.Failure("Invalid password");
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
                _logger.LogError(ex, "Login failed");
                return AuthResultInternalDto.Failure("Internal server error");
            }
        }

        public Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public async Task<AuthResultInternalDto> RegisterAsync(RegisterInternalDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!string.Equals(dto.Password, dto.ConfirmPassword, StringComparison.Ordinal))
                {
                    return AuthResultInternalDto.Failure("Passwords do not match");
                }

                var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
                var exists = await _context.Users
                    .AsNoTracking()
                    .AnyAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);

                if (exists)
                {
                    return AuthResultInternalDto.Failure("User with this email already exists");
                }

                var universityId = await EnsureUniversityAsync(dto.University, normalizedEmail, cancellationToken);
                var facultyId = await EnsureFacultyAsync(universityId, dto.Faculty, cancellationToken);
                var roleId = await EnsureRoleAsync("user", cancellationToken);

                var user = new User
                {
                    Email = normalizedEmail,
                    FullName = dto.FullName.Trim(),
                    PhoneNumber = dto.PhoneNumber.Trim(),
                    UniversityId = universityId,
                    FacultyId = facultyId,
                    RoleId = roleId,
                    Rating = 0,
                    ReviewsCount = 0,
                    IsBlocked = false,
                    IsEmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

                _context.Users.Add(user);
                await _context.SaveChangesAsync(cancellationToken);

                _context.Carts.Add(new Cart
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync(cancellationToken);

                user.Role = await _context.Roles
                    .AsNoTracking()
                    .FirstAsync(x => x.Id == roleId, cancellationToken);

                return AuthResultInternalDto.Success(MapUser(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed");
                return AuthResultInternalDto.Failure("Internal server error");
            }
        }

        public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            _ = await _context.Users
                .AsNoTracking()
                .AnyAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);
        }

        public async Task ResetPasswordAsync(string token, string newPassword, string confirmPassword, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token is required", nameof(token));
            }

            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            {
                throw new ArgumentException("Passwords do not match", nameof(confirmPassword));
            }

            var normalizedEmail = token.Trim().ToLowerInvariant();
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);

            if (user is null)
            {
                throw new KeyNotFoundException("User not found");
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
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

        private async Task<int> EnsureRoleAsync(string name, CancellationToken cancellationToken)
        {
            var normalizedName = name.Trim().ToLowerInvariant();
            var role = await _context.Roles.FirstOrDefaultAsync(x => x.Name.ToLower() == normalizedName, cancellationToken);
            if (role is not null)
            {
                return role.Id;
            }

            role = new Role
            {
                Name = normalizedName
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync(cancellationToken);
            return role.Id;
        }

        private async Task<int> EnsureUniversityAsync(string universityName, string email, CancellationToken cancellationToken)
        {
            var normalizedName = universityName.Trim();
            var normalizedNameLower = normalizedName.ToLowerInvariant();
            var domain = email.Contains('@') ? email[(email.IndexOf('@') + 1)..] : "university.local";

            var university = await _context.Universities
                .FirstOrDefaultAsync(x => x.Name.ToLower() == normalizedNameLower, cancellationToken);

            if (university is not null)
            {
                if (string.IsNullOrWhiteSpace(university.Domain))
                {
                    university.Domain = domain;
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return university.Id;
            }

            university = new University
            {
                Name = normalizedName,
                Domain = domain
            };

            _context.Universities.Add(university);
            await _context.SaveChangesAsync(cancellationToken);
            return university.Id;
        }

        private async Task<int> EnsureFacultyAsync(int universityId, string facultyName, CancellationToken cancellationToken)
        {
            var normalizedName = facultyName.Trim();
            var normalizedNameLower = normalizedName.ToLowerInvariant();

            var faculty = await _context.Faculties
                .FirstOrDefaultAsync(
                    x => x.UniversityId == universityId && x.Name.ToLower() == normalizedNameLower,
                    cancellationToken);

            if (faculty is not null)
            {
                return faculty.Id;
            }

            faculty = new Faculty
            {
                UniversityId = universityId,
                Name = normalizedName
            };

            _context.Faculties.Add(faculty);
            await _context.SaveChangesAsync(cancellationToken);
            return faculty.Id;
        }
    }
}
