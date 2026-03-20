using FunApi.Models.Advertisements;

namespace FunApi.Models.Carts
{
    public class CartItem
    {
        public long Id { get; set; }
        public long CartId { get; set; }
        public long AdvertisementId { get; set; }
        public DateTime CreatedAt { get; set; }

        public Cart Cart { get; set; } = null!;
        public Advertisement Advertisement { get; set; } = null!;
    }
}
