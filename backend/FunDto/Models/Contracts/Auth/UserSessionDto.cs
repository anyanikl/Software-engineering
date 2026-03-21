namespace FunDto.Models.Contracts.Auth
{
    public class UserSessionDto
    {
        public int Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
    }
}
