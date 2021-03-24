using Xunit;
using YesSql.Utils;

namespace YesSql.Tests
{
    public class HashHelperTests
    {
        private const string _longKeyName = "MyTablePrefix_FK_LongCollectionName_LongIndexTableName_LongCollectionName_Document_DocumentId";

        [Fact]
        public void ShouldBeDeterministic()
        {
            var first = HashHelper.HashName(_longKeyName);
            var second = HashHelper.HashName(_longKeyName);
            Assert.Equal(first, second);
        }

        [Fact]
        public void ShouldBe52Chars()
        {
            Assert.Equal(52, HashHelper.HashName(_longKeyName).Length);
        }
    }
}
