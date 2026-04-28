using FunApi.Exceptions;

namespace FunApi.Security
{
    public static class PasswordPolicy
    {
        public const int MinLength = 8;

        public static void EnsureValid(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new DomainValidationException("Password is required");
            }

            if (password.Length < MinLength)
            {
                throw new DomainValidationException($"Password must be at least {MinLength} characters long");
            }

            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            {
                throw new DomainValidationException("Password must contain at least one letter and one digit");
            }
        }
    }
}
