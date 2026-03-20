namespace FunDto.Models.Contracts.Chats
{
    public class ChatDto
    {
        public int Id { get; set; }
        public int AdvertisementId { get; set; }
        public string AdvertisementTitle { get; set; } = null!;
        public int BuyerId { get; set; }
        public string BuyerName { get; set; } = null!;
        public int SellerId { get; set; }
        public string SellerName { get; set; } = null!;
        public List<MessageDto> Messages { get; set; } = new();
    }
}
