using System.ComponentModel.DataAnnotations;

namespace FunDto.Models.Contracts.Moderation
{
    public class ModerationDecisionDto
    {
        [MaxLength(1000)]
        public string? Comment { get; set; }
    }
}
