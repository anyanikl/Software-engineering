using System.ComponentModel.DataAnnotations;

namespace FunDto.Models.Contracts.Auth
{
    public class RegisterRequestDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public string Password { get; set; } = null!;

        [Required]
        public string ConfirmPassword { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = null!;

        [Required]
        [MaxLength(30)]
        public string Phone { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string University { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string Faculty { get; set; } = null!;
    }
}
