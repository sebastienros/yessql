using System;
using YesSql.Indexes;

namespace YesSql.Tests.Indexes
{
    public class TypesIndex : MapIndex
    {
        //public char ValueChar { get; set; }
        public bool ValueBool { get; set; }
        //public sbyte ValueSByte { get; set; }
        public short ValueShort { get; set; }
        public ushort ValueUShort { get; set; }
        public int ValueInt { get; set; }
        public uint ValueUInt { get; set; }
        public long ValueLong { get; set; }
        public ulong ValueULong { get; set; }
        public float ValueFloat { get; set; }
        public double ValueDouble { get; set; }
        public decimal ValueDecimal { get; set; }
        public DateTime ValueDateTime { get; set; }
        //public DateTimeOffset ValueDateTimeOffset { get; set; }
        public Guid ValueGuid { get; set; }
        //public TimeSpan ValueTimeSpan { get; set; }

        public char? NullableChar { get; set; }
        public bool? NullableBool { get; set; }
        public sbyte? NullableSByte { get; set; }
        public short? NullableShort { get; set; }
        public ushort? NullableUShort { get; set; }
        public int? NullableInt { get; set; }
        public uint? NullableUInt { get; set; }
        public long? NullableLong { get; set; }
        public ulong? NullableULong { get; set; }
        public float? NullableFloat { get; set; }
        public double? NullableDouble { get; set; }
        public decimal? NullableDecimal { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public DateTimeOffset? NullableDateTimeOffset { get; set; }
        public Guid? NullableGuid { get; set; }
        public TimeSpan? NullableTimeSpan { get; set; }
    }
}
