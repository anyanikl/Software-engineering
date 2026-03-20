namespace FunDto.Models.Internal.Orders
{
    public class OrderInternalDto
    {
        public int Id { get; set; }
        public int AdvertisementId { get; set; }
        public int BuyerId { get; set; }
        public int SellerId { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
