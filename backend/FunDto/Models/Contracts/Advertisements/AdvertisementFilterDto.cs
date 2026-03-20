using System.ComponentModel.DataAnnotations;

namespace FunDto.Models.Contracts.Advertisements
{
    public class AdvertisementFilterDto
    {
        [MaxLength(255)]
        public string? Search { get; set; }

        public int? Course { get; set; }

        [MaxLength(50)]
        public string? Type { get; set; }

        [MaxLength(50)]
        public string? SortBy { get; set; }
    }
}
