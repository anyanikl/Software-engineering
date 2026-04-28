using System.Security.Cryptography;
using System.Text;
using FunApi.Exceptions;
using FunApi.Interfaces;
using FunApi.Models;
using FunApi.Models.Auth;
using FunApi.Models.Carts;
using FunApi.Models.Users;
using FunApi.Security;
using FunDto.Models.Internal.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace FunApi.Services
{
    public class AuthService : IAuthService
    {
        private const int EmailConfirmationLifetimeHours = 24;
        private const int PasswordResetLifetimeMinutes = 30;

        private readonly FunDBcontext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public AuthService(
            FunDBcontext context,
            ILogger<AuthService> logger,
            IEmailSender emailSender,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _passwordHasher = new PasswordHasher<User>();
            _emailSender = emailSender;
            _configuration = configuration;
        }

        public async Task ConfirmEmailAsync(string token, CancellationToken cancellationToken = default)
        {
            var tokenHash = HashToken(token);
            var now = DateTime.UtcNow;

            var user = await _context.Users
                .FirstOrDefaultAsync(
                    x => x.EmailConfirmationTokenHash == tokenHash
                        && x.EmailConfirmationTokenExpiresAt.HasValue
                        && x.EmailConfirmationTokenExpiresAt > now,
                    cancellationToken);

            if (user is null)
            {
                throw new DomainValidationException("Invalid or expired email confirmation token");
            }

            user.IsEmailConfirmed = true;
            user.EmailConfirmationTokenHash = null;
            user.EmailConfirmationTokenExpiresAt = null;
            user.UpdatedAt = now;

            await _context.SaveChangesAsync(cancellationToken);
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
                var normalizedEmail = NormalizeEmail(dto.Email);
                var user = await _context.Users
                    .Include(x => x.Role)
                    .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);

                if (user is null)
                {
                    return AuthResultInternalDto.Failure("Invalid email or password");
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
                    return AuthResultInternalDto.Failure("Invalid email or password");
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

                PasswordPolicy.EnsureValid(dto.Password);

                var normalizedEmail = NormalizeEmail(dto.Email);
                var exists = await _context.Users
                    .AsNoTracking()
                    .AnyAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);

                if (exists)
                {
                    return AuthResultInternalDto.Failure("User with this email already exists");
                }

                var universityName = NormalizeRequired(dto.University, "University");
                var facultyName = NormalizeRequired(dto.Faculty, "Faculty");
                var fullName = NormalizeRequired(dto.FullName, "Full name");
                var phoneNumber = NormalizeRequired(dto.PhoneNumber, "Phone number");

                var universityId = await EnsureUniversityAsync(universityName, normalizedEmail, cancellationToken);
                var facultyId = await EnsureFacultyAsync(universityId, facultyName, cancellationToken);
                var roleId = await EnsureRoleAsync(AppRoles.User, cancellationToken);

                var user = new User
                {
                    Email = normalizedEmail,
                    FullName = fullName,
                    PhoneNumber = phoneNumber,
                    UniversityId = universityId,
                    FacultyId = facultyId,
                    RoleId = roleId,
                    Rating = 0,
                    ReviewsCount = 0,
                    IsBlocked = false,
                    IsEmailConfirmed = false,
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
                await SendEmailConfirmationAsync(user, cancellationToken);

                user.Role = await _context.Roles
                    .AsNoTracking()
                    .FirstAsync(x => x.Id == roleId, cancellationToken);

                return AuthResultInternalDto.Success(MapUser(user));
            }
            catch (DomainValidationException validationException)
            {
                _logger.LogWarning(validationException, "Registration validation failed");
                return AuthResultInternalDto.Failure(validationException.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed");
                return AuthResultInternalDto.Failure("Internal server error");
            }
        }

        public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = NormalizeEmail(email);
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);

            if (user is null || user.IsBlocked || !user.IsEmailConfirmed)
            {
                return;
            }

            await SendPasswordResetEmailAsync(user, cancellationToken);
        }

        public async Task RequestEmailConfirmationAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = NormalizeEmail(email);
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);

            if (user is null || user.IsBlocked || user.IsEmailConfirmed)
            {
                return;
            }

            await SendEmailConfirmationAsync(user, cancellationToken);
        }

        public async Task ResetPasswordAsync(string token, string newPassword, string confirmPassword, CancellationToken cancellationToken = default)
        {
            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            {
                throw new DomainValidationException("Passwords do not match");
            }

            PasswordPolicy.EnsureValid(newPassword);

            var tokenHash = HashToken(token);
            var now = DateTime.UtcNow;

            var user = await _context.Users
                .FirstOrDefaultAsync(
                    x => x.PasswordResetTokenHash == tokenHash
                        && x.PasswordResetTokenExpiresAt.HasValue
                        && x.PasswordResetTokenExpiresAt > now,
                    cancellationToken);

            if (user is null)
            {
                throw new DomainValidationException("Invalid or expired password reset token");
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpiresAt = null;
            user.UpdatedAt = now;

            await _context.SaveChangesAsync(cancellationToken);
        }

        private static AuthUserInternalDto MapUser(User user)
        {
            return new AuthUserInternalDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role?.Name ?? string.Empty
            };
        }

        private async Task SendEmailConfirmationAsync(User user, CancellationToken cancellationToken)
        {
            var token = GenerateToken();
            user.EmailConfirmationTokenHash = HashToken(token);
            user.EmailConfirmationTokenExpiresAt = DateTime.UtcNow.AddHours(EmailConfirmationLifetimeHours);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            var confirmationUrl = BuildFrontendUrl("confirm-email.html", token);
            await _emailSender.SendAsync(
                user.Email,
                "Confirm your FinPay email",
                $"""
                <p>Welcome to FinPay.</p>
                <p>Confirm your email by opening the link below:</p>
                <p><a href="{confirmationUrl}">{confirmationUrl}</a></p>
                <p>The link expires in {EmailConfirmationLifetimeHours} hours.</p>
                """,
                cancellationToken);
        }

        private async Task SendPasswordResetEmailAsync(User user, CancellationToken cancellationToken)
        {
            var token = GenerateToken();
            user.PasswordResetTokenHash = HashToken(token);
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(PasswordResetLifetimeMinutes);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            var resetUrl = BuildFrontendUrl("reset-password.html", token);
            await _emailSender.SendAsync(
                user.Email,
                "Reset your FinPay password",
                $"""
                <p>You requested a password reset.</p>
                <p>Open the link below to set a new password:</p>
                <p><a href="{resetUrl}">{resetUrl}</a></p>
                <p>The link expires in {PasswordResetLifetimeMinutes} minutes.</p>
                """,
                cancellationToken);
        }

        private string BuildFrontendUrl(string relativePage, string token)
        {
            var frontendBaseUrl = _configuration["Frontend:BaseUrl"]
                ?? _configuration["FrontendBaseUrl"]
                ?? "https://localhost";

            frontendBaseUrl = frontendBaseUrl.TrimEnd('/') + "/";

            var uri = new Uri(new Uri(frontendBaseUrl), relativePage);
            var separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&";
            return $"{uri}{separator}token={Uri.EscapeDataString(token)}";
        }

        private static string GenerateToken()
        {
            return WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        }

        private static string HashToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new DomainValidationException("Token is required");
            }

            var bytes = Encoding.UTF8.GetBytes(token.Trim());
            return Convert.ToHexString(SHA256.HashData(bytes));
        }

        private static string NormalizeEmail(string email)
        {
            var normalizedEmail = NormalizeRequired(email, "Email").ToLowerInvariant();
            return normalizedEmail;
        }

        private static string NormalizeRequired(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainValidationException($"{fieldName} is required");
            }

            return value.Trim();
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
            var normalizedNameLower = universityName.ToLowerInvariant();
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
                Name = universityName,
                Domain = domain
            };

            _context.Universities.Add(university);
            await _context.SaveChangesAsync(cancellationToken);
            return university.Id;
        }

        private async Task<int> EnsureFacultyAsync(int universityId, string facultyName, CancellationToken cancellationToken)
        {
            var normalizedNameLower = facultyName.ToLowerInvariant();

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
                Name = facultyName
            };

            _context.Faculties.Add(faculty);
            await _context.SaveChangesAsync(cancellationToken);
            return faculty.Id;
        }
    }
}
