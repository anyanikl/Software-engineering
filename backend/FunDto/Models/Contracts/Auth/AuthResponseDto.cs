namespace FunDto.Models.Contracts.Auth
{
    public class AuthResponseDto
    {
        public bool IsSuccess { get; init; }
        public UserSessionDto? User { get; init; }
        public bool RequiresEmailConfirmation { get; init; }
        public string? Message { get; init; }
        public List<string> Errors { get; init; } = new();
    }
}
