using System.ComponentModel.DataAnnotations;

namespace FunDto.Models.Contracts.Reviews
{
    public class CreateReviewDto
    {
        public int OrderId { get; set; }

        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }
}
