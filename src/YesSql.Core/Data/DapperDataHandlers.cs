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
            if (value is string s)
            {
                return DateTimeOffset.Parse(s);
            }
            else if (value is DateTimeOffset d)
            {
                return d;
            }

            return Convert.ToDateTime(value);
        }            
    }

    class GuidHandler : DapperTypeHandler<Guid>
    {
        public override Guid Parse(object value)
        {
            if (value is string s)
            {
                return Guid.Parse(s);
            }
            else if (value is Guid g)
            {
                return g;
            }

            return Guid.Empty;
        }
    }

    class TimeSpanHandler : DapperTypeHandler<TimeSpan>
    {
        public override TimeSpan Parse(object value)
        {
            if (value is string s)
            {
                return TimeSpan.Parse(s);
            }
            else if (value is TimeSpan t)
            {
                return t;
            }

            return TimeSpan.Zero;
        }
    }
}
