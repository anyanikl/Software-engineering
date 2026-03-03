using System.ComponentModel.DataAnnotations;

namespace FunDto.Models
{
    public class RegisterDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Password2 { get; set; }
    }
}
