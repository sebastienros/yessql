using Xunit;
using YesSql.Services;

namespace YesSql.Tests
{
    public class TypeServiceTests
    {
        [SimplifiedTypeName("CustomName")]
        public class TypeWithSimplifiedName
        {
        }

        public class PublicType
        {
        }

        private class PrivateType
        {
        }

        [Fact]
        public void ShouldResolveAnonymousTypeAsDynamic()
        {
            var typeService = new TypeService();
            var anonymous = new { Name = "Bob", Age = 42 };

            Assert.Equal("dynamic", typeService[anonymous.GetType()]);
        }

        [Fact]
        public void ShouldNotResolvePublicTypeAsDynamic()
        {
            var typeService = new TypeService();

            Assert.NotEqual("dynamic", typeService[typeof(PublicType)]);
        }

        [Fact]
        public void ShouldNotResolveNonPublicTypeAsDynamic()
        {
            var typeService = new TypeService();

            Assert.NotEqual("dynamic", typeService[typeof(PrivateType)]);
        }

        [Fact]
        public void ShouldUseSimplifiedTypeName()
        {
            var typeService = new TypeService();

            Assert.Equal("CustomName", typeService[typeof(TypeWithSimplifiedName)]);
        }

        [Fact]
        public void ShouldRoundTripTypeName()
        {
            var typeService = new TypeService();

            var name = typeService[typeof(PublicType)];

            Assert.Equal(typeof(PublicType), typeService[name]);
        }
    }
}
