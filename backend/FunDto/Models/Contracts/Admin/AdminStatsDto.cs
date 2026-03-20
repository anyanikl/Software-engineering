namespace FunDto.Models.Contracts.Admin
{
    public class AdminStatsDto
    {
        public long Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsBlocked { get; set; }
    }
}
