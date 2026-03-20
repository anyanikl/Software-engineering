using FunApi.Models.Users;

namespace FunApi.Models.Carts
{
    public class Cart
    {
        public long Id { get; set; }
        public long UserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User User { get; set; } = null!;
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
