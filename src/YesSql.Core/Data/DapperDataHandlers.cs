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

    class DateOnlyHandler : DapperTypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            // Convert DateOnly to DateTime for database storage
            parameter.Value = value.ToDateTime(TimeOnly.MinValue);
        }

        public override DateOnly Parse(object value)
        {
            switch (value)
            {
                case null:
                    return DateOnly.MinValue;

                case string s:
                    // Try parsing as DateTime first to handle formats like "2024-03-15 00:00:00"
                    if (DateTime.TryParse(s, out var dateTime))
                    {
                        return DateOnly.FromDateTime(dateTime);
                    }
                    return DateOnly.Parse(s);

                case DateTime dt:
                    return DateOnly.FromDateTime(dt);

                case DateOnly d:
                    return d;

                default:
                    return DateOnly.MinValue;
            }
        }
    }

    class TimeOnlyHandler : DapperTypeHandler<TimeOnly>
    {
        public override void SetValue(IDbDataParameter parameter, TimeOnly value)
        {
            // Convert TimeOnly to TimeSpan for database storage
            var timeSpan = value.ToTimeSpan();
            
            // For parameters that will be stored in DateTime columns (like SQL Server's DATETIME),
            // convert to DateTime to avoid overflow issues with ticks
            if (parameter.DbType == DbType.DateTime || parameter.DbType == DbType.DateTime2 || 
                parameter.DbType == DbType.Date || parameter.DbType == DbType.Time)
            {
                // Use 1900-01-01 as base date for SQL Server compatibility
                parameter.Value = new DateOnly(1900, 1, 1).ToDateTime(value);
            }
            else
            {
                parameter.Value = timeSpan;
            }
        }

        public override TimeOnly Parse(object value)
        {
            switch (value)
            {
                case null:
                    return TimeOnly.MinValue;

                case string s:
                    return TimeOnly.Parse(s);

                case TimeSpan ts:
                    return TimeOnly.FromTimeSpan(ts);

                case DateTime dt:
                    return TimeOnly.FromDateTime(dt);

                case TimeOnly t:
                    return t;

                default:
                    return TimeOnly.MinValue;
            }
        }
    }
}
