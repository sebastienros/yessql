using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class AttachmentByDay : ReduceIndex
    {
        public int Date { get; set; }
        public int Count { get; set; }
    }

    public class AttachmentByDayProvider : IndexProvider<Email>
    {
        public override void Describe(DescribeContext<Email> context)
        {
            context
                .For<AttachmentByDay, int>()
                    /* 
                     * This syntax is equivalent
                     * 
                    .Map(email => email.Attachements.Select(a => new AttachmentByDay
                    {
                        Date = email.Date.DayOfYear,
                        Count = 1
                    })) 
                    */
                    .Map(email => new AttachmentByDay
                    {
                        Date = email.Date.DayOfYear,
                        Count = email.Attachments.Count()
                    })
                    .Group(email => email.Date)
                    .Reduce(group => new AttachmentByDay
                    {
                        Date = group.Key,
                        Count = group.Sum(y => y.Count)
                    })
                    .Delete((index, map) =>
                    {
                        index.Count -= map.Sum(x => x.Count);

                        // if Count == 0 then delete the index
                        return index.Count > 0 ? index : null;
                    });
        }
    }
}
