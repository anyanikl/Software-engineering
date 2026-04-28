namespace FunDto.Models.Contracts.Orders
{
    public class OrderDto
    {
        public int Id { get; set; }
        public int AdvertisementId { get; set; }
        public string AdvertisementTitle { get; set; } = null!;
        public int BuyerId { get; set; }
        public string BuyerName { get; set; } = null!;
        public string SellerName { get; set; } = null!;
        public decimal Price { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
