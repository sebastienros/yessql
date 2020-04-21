using Xunit;
using YesSql.Naming;

namespace YesSql.Tests
{
    public class NamingCaseTests
    {
        private NamingCaseProvider _namingCaseProvider;
        
        [Theory]
        [InlineData("Document", "Document")]
        [InlineData("DocumentId", "DocumentId")]
        [InlineData("DisplayName", "DisplayName")]
        public void PascalCase_CorrectOutput(string input, string expectedOutput)
        {
            _namingCaseProvider = new NamingCaseProvider(NamingCase.PascalCase);

            var output = _namingCaseProvider.GetName(input);

            Assert.Equal(expectedOutput, output);
        }

        [Theory]
        [InlineData("Document", "document")]
        [InlineData("DocumentId", "documentId")]
        [InlineData("DisplayName", "displayName")]
        public void CamelCase_CorrectOutput(string input, string expectedOutput)
        {
            _namingCaseProvider = new NamingCaseProvider(NamingCase.CamelCase);

            var output = _namingCaseProvider.GetName(input);

            Assert.Equal(expectedOutput, output);
        }

        [Theory]
        [InlineData("Document", "document")]
        [InlineData("DocumentId", "document_id")]
        [InlineData("DisplayName", "display_name")]
        public void SnakeCase_CorrectOutput(string input, string expectedOutput)
        {
            _namingCaseProvider = new NamingCaseProvider(NamingCase.SnakeCase);

            var output = _namingCaseProvider.GetName(input);

            Assert.Equal(expectedOutput, output);
        }
    }
}
