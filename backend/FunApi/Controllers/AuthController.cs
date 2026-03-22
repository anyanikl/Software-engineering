using FunApi.Interfaces;
using FunDto.Models.Contracts.Auth;
using FunDto.Models.Internal.Auth;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FunApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IAntiforgery _antiforgery;
        private readonly IWebHostEnvironment _environment;

        public AuthController(
            IAuthService authService,
            IAntiforgery antiforgery,
            IWebHostEnvironment environment)
        {
            _authService = authService;
            _antiforgery = antiforgery;
            _environment = environment;
        }

        [HttpGet("csrf")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public ActionResult<CsrfTokenDto> GetCsrfToken()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            if (string.IsNullOrWhiteSpace(tokens.RequestToken))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new CsrfTokenDto());
            }

            Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions
            {
                HttpOnly = false,
                IsEssential = true,
                SameSite = SameSiteMode.None,
                Secure = !_environment.IsDevelopment()
            });

            return Ok(new CsrfTokenDto { Token = tokens.RequestToken });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var result = await _authService.LoginAsync(new LoginInternalDto
            {
                Email = request.Email,
                Password = request.Password
            }, cancellationToken);

            if (!result.IsSuccess || result.User is null)
            {
                return Unauthorized(new AuthResponseDto
                {
                    IsSuccess = false,
                    Errors = result.Errors
                });
            }

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                BuildPrincipal(result.User),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    AllowRefresh = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                });

            return Ok(ToContract(result.User));
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var result = await _authService.RegisterAsync(new RegisterInternalDto
            {
                Email = request.Email,
                Password = request.Password,
                ConfirmPassword = request.ConfirmPassword,
                FullName = request.FullName,
                PhoneNumber = request.Phone,
                University = request.University,
                Faculty = request.Faculty
            }, cancellationToken);

            if (!result.IsSuccess || result.User is null)
            {
                return BadRequest(new AuthResponseDto
                {
                    IsSuccess = false,
                    Errors = result.Errors
                });
            }

            return Ok(ToContract(result.User));
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            await _authService.RequestPasswordResetAsync(request.Email, cancellationToken);
            return NoContent();
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                await _authService.ResetPasswordAsync(
                    request.Token,
                    request.NewPassword,
                    request.ConfirmPassword,
                    cancellationToken);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            await _authService.LogoutAsync(cancellationToken);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            Response.Cookies.Delete("XSRF-TOKEN", new CookieOptions
            {
                SameSite = SameSiteMode.None,
                Secure = !_environment.IsDevelopment()
            });

            return NoContent();
        }

        [HttpGet("me")]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<AuthResponseDto>> Me(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new AuthResponseDto
                {
                    IsSuccess = false,
                    Errors = new List<string> { "Invalid session" }
                });
            }

            var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);
            if (user is null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Unauthorized(new AuthResponseDto
                {
                    IsSuccess = false,
                    Errors = new List<string> { "User not found" }
                });
            }

            return Ok(ToContract(user));
        }

        private static ClaimsPrincipal BuildPrincipal(AuthUserInternalDto user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new("Id", user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role)
            };

            return new ClaimsPrincipal(new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme));
        }

        private static AuthResponseDto ToContract(AuthUserInternalDto user)
        {
            return new AuthResponseDto
            {
                IsSuccess = true,
                User = new UserSessionDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role
                }
            };
        }
    }
}
