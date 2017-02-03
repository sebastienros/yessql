using System;
using System.Data;
using Dapper;

namespace YesSql.Core.Services
{
    public class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public static readonly DateTimeOffsetHandler Default = new DateTimeOffsetHandler();
        private DateTimeOffsetHandler()
        {

        }
        public override DateTimeOffset Parse(object value)
        {
            if (value is DateTime)
            {
                return DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
            }

            return (DateTimeOffset)value;
        }

        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
        {
            if (value == null)
            {
                parameter.Value = null;
            }
            else
            {
                parameter.Value = value.UtcDateTime;
            }
        }
    }
}
