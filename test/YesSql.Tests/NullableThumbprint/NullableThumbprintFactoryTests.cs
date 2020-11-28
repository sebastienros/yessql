using System;
using System.Linq;
using Xunit;
using YesSql.Data;

namespace YesSql.Tests.NullableThumbprint
{
    public class NullableThumpbrintFactoryTests
    {
        [Fact]
        public void ShouldReturnSameInstancePerType()
        {
            var d1 = NullableThumbprintFactory.GetNullableThumbprintBuilder(typeof(DiscriminatorWithStrings));
            var d2 = NullableThumbprintFactory.GetNullableThumbprintBuilder(typeof(DiscriminatorWithStrings));

            Assert.NotNull(d1);
            Assert.NotNull(d2);
            Assert.Same(d1, d2);
        }

        [Fact]
        public void ShouldReturnDifferentInstances()
        {
            var d1 = NullableThumbprintFactory.GetNullableThumbprintBuilder(typeof(DiscriminatorWithNoNullable));
            var d2 = NullableThumbprintFactory.GetNullableThumbprintBuilder(typeof(DiscriminatorWithNoNullable2));

            Assert.NotNull(d1);
            Assert.NotNull(d2);
            Assert.NotEqual(d1, d2);
        }

        [Fact]
        public void ShouldReturnDifferentValues()
        {
            var a1 = NullableThumbprintFactory.GetNullableThumbprint(new DiscriminatorWithNoNullable());
            var a2 = NullableThumbprintFactory.GetNullableThumbprint(new DiscriminatorWithNoNullable2());
            
            Assert.NotEqual(a1, a2);
        }

        [Fact]
        public void ShouldReturnSameMaskForDifferentInstances()
        {
            var d = NullableThumbprintFactory.GetNullableThumbprintBuilder(typeof(DiscriminatorWithNoNullable));

            var a1 = d.GetNullableThumbprint(new DiscriminatorWithNoNullable { A = 0, B = true, C = DateTime.UtcNow });
            var a2 = d.GetNullableThumbprint(new DiscriminatorWithNoNullable { A = 1, B = false });

            Assert.Equal(a1, a2);
        }

        [Fact]
        public void ShouldNotHaveLsbSetForNonNullable()
        {
            var d = NullableThumbprintFactory.GetNullableThumbprintBuilder(typeof(DiscriminatorWithNoNullable));

            var a1 = d.GetNullableThumbprint(new DiscriminatorWithNoNullable());
            var lsb = a1 & uint.MaxValue;

            Assert.Equal(0, lsb);
        }

        [Fact]
        public void ShouldBuildDiscriminatorForString()
        {
            var d1 = NullableThumbprintFactory.GetNullableThumbprintBuilder(typeof(DiscriminatorWithStrings));

            var a1 = d1.GetNullableThumbprint(new DiscriminatorWithStrings { A = "a", B = "", C = "", D = "" });
            var a2 = d1.GetNullableThumbprint(new DiscriminatorWithStrings { A = "b", B = "", C = "", D = "" });
            var a3 = d1.GetNullableThumbprint(new DiscriminatorWithStrings { A = null, B = "", C = "", D = "" });
            var a4 = d1.GetNullableThumbprint(new DiscriminatorWithStrings { A = null, B = "b", C = "", D = "" });
            var a5 = d1.GetNullableThumbprint(new DiscriminatorWithStrings { A = null, B = "", C = null, D = "" });
            var a6 = d1.GetNullableThumbprint(new DiscriminatorWithStrings { A = null, B = "b", C = null, D = "b" });
            var a7 = d1.GetNullableThumbprint(new DiscriminatorWithStrings { A = null, B = null, C = "", D = "" });
            var a8 = d1.GetNullableThumbprint(new DiscriminatorWithStrings { A = "", B = "", C = null, D = null });

            Assert.Equal(a1, a2);
            Assert.Equal(a3, a4);
            Assert.NotEqual(a1, a3);
            Assert.Equal(a5, a6);
            var distinctValues = new[] { a1, a2, a3, a4, a5, a6, a7, a8 }.Distinct();
            Assert.Equal(5, distinctValues.Count());
        }

        [Fact]
        public void ShouldBuildDiscriminatorForNullableTypes()
        {
            var d1 = NullableThumbprintFactory.GetNullableThumbprintBuilder(typeof(DiscriminatorWithNullable));

            var a1 = d1.GetNullableThumbprint(new DiscriminatorWithNullable { A = 0, B = true, C = DateTime.UtcNow });
            var a2 = d1.GetNullableThumbprint(new DiscriminatorWithNullable { A = 1, B = false, C = DateTime.UtcNow });
            var a3 = d1.GetNullableThumbprint(new DiscriminatorWithNullable { A = null, B = true, C = DateTime.UtcNow });
            var a4 = d1.GetNullableThumbprint(new DiscriminatorWithNullable { A = null, B = false, C = DateTime.UtcNow });
            var a5 = d1.GetNullableThumbprint(new DiscriminatorWithNullable { A = null, B = true, C = null });
            var a6 = d1.GetNullableThumbprint(new DiscriminatorWithNullable { A = null, B = false, C = null });
            var a7 = d1.GetNullableThumbprint(new DiscriminatorWithNullable { A = null, B = null, C = DateTime.UtcNow });
            var a8 = d1.GetNullableThumbprint(new DiscriminatorWithNullable { A = 0, B = null, C = null });

            Assert.Equal(a1, a2);
            Assert.Equal(a3, a4);
            Assert.NotEqual(a1, a3);
            Assert.Equal(a5, a6);
            var distinctValues = new[] { a1, a2, a3, a4, a5, a6, a7, a8 }.Distinct();
            Assert.Equal(5, distinctValues.Count());
        }
    }
}
