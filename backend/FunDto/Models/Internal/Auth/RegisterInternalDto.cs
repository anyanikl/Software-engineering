namespace FunDto.Models.Internal.Auth
{
    public class RegisterInternalDto
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string ConfirmPassword { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string University { get; init; } = string.Empty;
        public string Faculty { get; init; } = string.Empty;
    }
}
