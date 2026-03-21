namespace FunDto.Models.Internal.Auth
{
    public class AuthResultInternalDto
    {
        public bool IsSuccess { get; init; }
        public AuthUserInternalDto? User { get; init; }
        public List<string> Errors { get; init; } = new();

        public static AuthResultInternalDto Success(AuthUserInternalDto user)
        {
            return new AuthResultInternalDto
            {
                IsSuccess = true,
                User = user
            };
        }

        public static AuthResultInternalDto Failure(params string[] errors)
        {
            return new AuthResultInternalDto
            {
                IsSuccess = false,
                Errors = errors.ToList()
            };
        }
    }
}
