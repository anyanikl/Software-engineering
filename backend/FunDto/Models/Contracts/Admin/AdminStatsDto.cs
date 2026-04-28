namespace FunDto.Models.Contracts.Admin
{
    public class AdminStatsDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = null!;
        public double Rating { get; set; }
        public int DealsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsBlocked { get; set; }
    }
}
