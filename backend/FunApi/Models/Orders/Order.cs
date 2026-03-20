using FunApi.Models.Advertisements;
using FunApi.Models.Users;

namespace FunApi.Models.Orders
{
    public class Order
    {
        public int Id { get; set; }

        public int AdvertisementId { get; set; }
        public int BuyerId { get; set; }
        public int SellerId { get; set; }
        public int OrderStatusId { get; set; }

        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public Advertisement Advertisement { get; set; } = null!;
        public User Buyer { get; set; } = null!;
        public User Seller { get; set; } = null!;
        public OrderStatus OrderStatus { get; set; } = null!;

        public Review? Review { get; set; }
    }
}
