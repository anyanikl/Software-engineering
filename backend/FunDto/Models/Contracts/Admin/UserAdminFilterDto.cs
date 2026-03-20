using System.ComponentModel.DataAnnotations;

namespace FunDto.Models.Contracts.Admin
{
    public class UserAdminFilterDto
    {
        [MaxLength(255)]
        public string? Search { get; set; }

        public bool? IsBlocked { get; set; }
    }
}
