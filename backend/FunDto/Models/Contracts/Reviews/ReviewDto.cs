namespace FunDto.Models.Contracts.Reviews
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public string AuthorName { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
