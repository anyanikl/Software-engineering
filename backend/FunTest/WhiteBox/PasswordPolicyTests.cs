using FunApi.Exceptions;
using FunApi.Security;

namespace FunTest.WhiteBox
{
    public class PasswordPolicyTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("        ")]
        public void EnsureValid_Throws_WhenPasswordIsBlank(string password)
        {
            Assert.Throws<DomainValidationException>(() => PasswordPolicy.EnsureValid(password));
        }

        [Fact]
        public void EnsureValid_Throws_WhenPasswordIsTooShort()
        {
            Assert.Throws<DomainValidationException>(() => PasswordPolicy.EnsureValid("a1b2"));
        }

        [Theory]
        [InlineData("abcdefgh")]
        [InlineData("12345678")]
        public void EnsureValid_Throws_WhenPasswordDoesNotContainLettersAndDigits(string password)
        {
            Assert.Throws<DomainValidationException>(() => PasswordPolicy.EnsureValid(password));
        }

        [Fact]
        public void EnsureValid_AllowsPasswordWithMinimumLengthLettersAndDigits()
        {
            PasswordPolicy.EnsureValid("pass1234");
        }
    }
}
