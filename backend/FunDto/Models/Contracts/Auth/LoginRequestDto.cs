using System.ComponentModel.DataAnnotations;

namespace FunDto.Models.Contracts.Auth
{
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Password { get; set; } = null!;
    }
}
