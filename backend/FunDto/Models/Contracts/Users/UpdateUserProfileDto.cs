using FunDto.Models.Contracts.Advertisements;
using FunDto.Models.Contracts.Reviews;
using System.ComponentModel.DataAnnotations;

namespace FunDto.Models.Contracts.Users
{
    public class UpdateUserProfileDto
    {
        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = null!;

        [Required]
        [MaxLength(30)]
        public string Phone { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string Faculty { get; set; } = null!;
    }
}
