using System.ComponentModel.DataAnnotations;

namespace FunDto.Models.Contracts.Auth
{
    public class ResetPasswordRequestDto
    {
        [Required]
        [MaxLength(500)]
        public string Token { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string NewPassword { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string ConfirmPassword { get; set; } = null!;
    }
}
