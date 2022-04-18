using System.Collections.Generic;
using Xunit;
using YesSql.Data;

namespace YesSql.Tests
{
    public class WorkerQueryKeyTests
    {
        [Fact]
        public void KeysWithDifferentPrefixShouldNotBeEqual()
        {
            var key1 = new WorkerQueryKey("prefix1", new[] { 1L });
            var key2 = new WorkerQueryKey("prefix2", new[] { 1L });

            Assert.False(key1.Equals(key2));
        }

        [Fact]
        public void KeysWithSamePrefixShouldBeEqual()
        {
            var key1 = new WorkerQueryKey("prefix1", new[] { 1L });
            var key2 = new WorkerQueryKey("prefix1", new[] { 1L });

            Assert.True(key1.Equals(key2));

            var params1 = new Dictionary<string, object> { ["a"] = 1, ["b"] = 1 };
            var params2 = new Dictionary<string, object> { ["a"] = 1, ["b"] = 1 };

            key1 = new WorkerQueryKey("prefix1", params1);
            key2 = new WorkerQueryKey("prefix1", params2);

            Assert.True(key1.Equals(key2));
        }

        [Fact]
        public void KeysWithDifferentIdsShouldNotBeEqual()
        {
            var key1 = new WorkerQueryKey("prefix1", new[] { 1L });
            var key2 = new WorkerQueryKey("prefix1", new[] { 2L });

            Assert.False(key1.Equals(key2));
        }

        [Fact]
        public void KeysWithDifferentParameterValuesShouldNotBeEqual()
        {
            var params1 = new Dictionary<string, object> { ["a"] = 1, ["b"] = 1 };
            var params2 = new Dictionary<string, object> { ["a"] = 1, ["b"] = 2 };

            var key1 = new WorkerQueryKey("prefix1", params1);
            var key2 = new WorkerQueryKey("prefix1", params2);

            Assert.False(key1.Equals(key2));
        }

        [Fact]
        public void KeysWithDifferentParameterCountShouldNotBeEqual()
        {
            var params1 = new Dictionary<string, object> { ["a"] = 1, ["b"] = 1 };
            var params2 = new Dictionary<string, object> { ["a"] = 1 };

            var key1 = new WorkerQueryKey("prefix1", params1);
            var key2 = new WorkerQueryKey("prefix1", params2);

            Assert.False(key1.Equals(key2));
        }

        [Fact]
        public void KeysWithDifferentParameterNamesShouldNotBeEqual()
        {
            var params1 = new Dictionary<string, object> { ["a"] = 1, ["b"] = 1 };
            var params2 = new Dictionary<string, object> { ["a"] = 1, ["c"] = 1 };

            var key1 = new WorkerQueryKey("prefix1", params1);
            var key2 = new WorkerQueryKey("prefix1", params2);

            Assert.False(key1.Equals(key2));
        }
    }
}
