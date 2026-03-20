using System.ComponentModel.DataAnnotations;

namespace FunDto.Models.Contracts.Chats
{
    public class SendMessageDto
    {
        [Required]
        public int ChatId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = null!;
    }
}
