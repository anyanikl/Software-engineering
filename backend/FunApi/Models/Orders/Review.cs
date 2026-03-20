using FunApi.Models.Users;

namespace FunApi.Models.Orders
{
    public class Review
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public int AuthorId { get; set; }
        public int TargetUserId { get; set; }

        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        public Order Order { get; set; } = null!;
        public User Author { get; set; } = null!;
        public User TargetUser { get; set; } = null!;
    }
}
