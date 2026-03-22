using System.ComponentModel.DataAnnotations;

namespace FunDto.Models.Contracts.Auth
{
    public class ForgotPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = null!;
    }
}
