using System;
using Xunit;
using Xunit.Extensions;
using YesSql.Core.Data.Models;
using YesSql.Core.Serialization;
using YesSql.Tests.Models;

namespace YesSql.Tests
{
    public class DocumentSerializersTests
    {
        [InlineData(typeof(JSonSerializer))]
        [InlineData(typeof(JSonNetSerializer))]
        [Theory]
        public void ShouldDeserializeAnonymousType(Type serializerType)
        {
            var serializer = Activator.CreateInstance(serializerType) as IDocumentSerializer;

            Assert.NotNull(serializer);

            const string content = @"{
                Firstname : 'Bill',
                Lastname : 'Gates',
                Address : {
                    Street : '1 Microsoft Way',
                    City : 'Redmond'
                }
            }";

            var doc = new Document
            {
                Content = content,
                Id = 0,
                Type = string.Empty
            };

            dynamic bill = serializer.Deserialize(doc);

            Assert.NotNull(bill);
            Assert.Equal("Bill", (string)bill.Firstname);
            Assert.Equal("Gates", (string)bill.Lastname);

            Assert.NotNull(bill.Address);
            Assert.Equal("1 Microsoft Way", (string)bill.Address.Street);
            Assert.Equal("Redmond", (string)bill.Address.City);

        }

        [InlineData(typeof(JSonSerializer))]
        [InlineData(typeof(JSonNetSerializer))]
        [Theory]
        public void ShouldDeserializeType(Type serializerType)
        {
            var serializer = Activator.CreateInstance(serializerType) as IDocumentSerializer;

            Assert.NotNull(serializer);

            const string content =
                @"{
                Firstname : 'Bill',
                Lastname : 'Gates',
                Address : {
                    Street : '1 Microsoft Way',
                    City : 'Redmond'
                }
            }";

            var doc = new Document
            {
                Content = content,
                Id = 0,
                Type = typeof (Person).SimplifiedTypeName()
            };

            var bill = serializer.Deserialize(doc) as Person;

            Assert.NotNull(bill);
            Assert.Equal("Bill", bill.Firstname);
            Assert.Equal("Gates", bill.Lastname);
        }

        [InlineData(typeof(JSonSerializer))]
        [InlineData(typeof(JSonNetSerializer))]
        [Theory]
        public void ShouldThrowWhenNull(Type serializerType)
        {
            var serializer = Activator.CreateInstance(serializerType) as IDocumentSerializer;
            
            var doc = new Document();
            Assert.Throws<ArgumentNullException>(() => serializer.Serialize(null, ref doc));
        }

        [InlineData(typeof(JSonSerializer))]
        [InlineData(typeof(JSonNetSerializer))]
        [Theory]
        public void ShouldReturnNullWhenEmpty(Type serializerType) {
            var serializer = Activator.CreateInstance(serializerType) as IDocumentSerializer;

            var doc = new Document {Content = String.Empty};
            Assert.Null(serializer.Deserialize(doc));
        }
    }
}
