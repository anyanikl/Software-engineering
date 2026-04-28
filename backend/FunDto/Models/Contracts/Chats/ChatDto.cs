namespace FunDto.Models.Contracts.Chats
{
    public class ChatDto
    {
        public int Id { get; set; }
        public int AdvertisementId { get; set; }
        public string AdvertisementTitle { get; set; } = null!;
        public int InterlocutorId { get; set; }
        public string InterlocutorName { get; set; } = null!;
        public List<MessageDto> Messages { get; set; } = new();
    }
}
