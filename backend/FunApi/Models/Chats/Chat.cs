using FunApi.Models.Advertisements;
using FunApi.Models.Users;

namespace FunApi.Models.Chats
{
    public class Chat
    {
        public int Id { get; set; }

        public int AdvertisementId { get; set; }
        public int BuyerId { get; set; }
        public int SellerId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Advertisement Advertisement { get; set; } = null!;
        public User Buyer { get; set; } = null!;
        public User Seller { get; set; } = null!;

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
