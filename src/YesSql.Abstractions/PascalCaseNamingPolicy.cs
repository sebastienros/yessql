using System;
using System.Threading;

namespace YesSql
{
    public class PascalCaseNamingPolicy : NamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("The name can't be null or empty.", nameof(name));
            }

            return Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(name.Replace(" ", string.Empty));
        }
    }
}
