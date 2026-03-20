using System.ComponentModel.DataAnnotations;

namespace FunDto.Models.Contracts.Advertisements
{
    public class UpdateAdvertisementDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        public int Course { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = null!;

        public decimal Price { get; set; }

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = null!;
    }
}
