using Dapper;
using System;
using System.Data;
using System.Globalization;

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
                    // Values stored in TEXT columns (e.g. Sqlite) don't keep the offset and are
                    // persisted as UTC. Treat offset-less strings as UTC so the instant round-trips
                    // regardless of the local timezone. Strings that carry an explicit offset
                    // (e.g. MySql 'O' format) keep their offset.
                    return DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

                case DateTime dt:
                    // DateTime values are stored as UTC. A value read back as Unspecified must be
                    // interpreted as UTC, otherwise the local timezone would shift the instant.
                    return new DateTimeOffset(dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt);

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
