namespace FunDto.Models.Contracts.Reviews
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = null!;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
