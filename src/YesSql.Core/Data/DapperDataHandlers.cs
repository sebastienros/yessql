using Dapper;
using System;
using System.Data;

namespace YesSql.Data
{
    abstract class DapperTypeHandler<T> : SqlMapper.TypeHandler<T>
    {
        public override void SetValue(IDbDataParameter parameter, T value)
            => parameter.Value = value;
    }

    class DateTimeOffsetHandler : DapperTypeHandler<DateTimeOffset>
    {
        public override DateTimeOffset Parse(object value)
        {
            switch (value)
            {
                case null:
                    return DateTimeOffset.MinValue;

                case string s:
                    return DateTimeOffset.Parse(s);

                case DateTime dt:
                    return new DateTimeOffset(dt);

                case DateTimeOffset d:
                    return d;

                default:
                    return DateTimeOffset.MinValue;
            }
        }
    }

    class GuidHandler : DapperTypeHandler<Guid>
    {
        public override Guid Parse(object value)
        {
            switch (value)
            {
                case null:
                    return Guid.Empty;

                case string s:
                    return Guid.Parse(s);

                case Guid g:
                    return g;

                default:
                    return Guid.Empty;
            }
        }
    }

    class TimeSpanHandler : DapperTypeHandler<TimeSpan>
    {
        public override TimeSpan Parse(object value)
        {
            switch (value)
            {
                case null:
                    return TimeSpan.Zero;

                case string s:
                    return TimeSpan.Parse(s);

                case long l:
                    return TimeSpan.FromTicks(l);

                case TimeSpan t:
                    return t;

                default:
                    return TimeSpan.Zero;
            }
        }
    }
}
