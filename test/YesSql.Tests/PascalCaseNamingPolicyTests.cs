using Xunit;

namespace YesSql.Tests
{
    public class PascalCaseNamingPolicyTests
    {
        [Theory]
        [InlineData("users", "Users")]
        [InlineData("Users In Roles", "UsersInRoles")]
        [InlineData("users in roles", "UsersInRoles")]
        [InlineData("FK_userId", "FK_userId")]
        [InlineData("fK_UserId", "FK_UserId")]
        public void ToPascalCase(string value, string expected)
        {
            // Arrange & Act
            var pascalCase = NamingPolicy.PascalCase.ConvertName(value);

            // Assert
            Assert.Equal(expected, pascalCase);
        }
    }
}
