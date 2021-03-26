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
            var first = HashHelper.HashName("FK_", _longKeyName);
            var second = HashHelper.HashName("FK_", _longKeyName);
            Assert.Equal(first, second);
        }

        [Fact]
        public void ShouldBe55Chars()
        {
            // 52 + FK_ = 55
            Assert.Equal(55, HashHelper.HashName("FK_", _longKeyName).Length);
        }
    }
}
