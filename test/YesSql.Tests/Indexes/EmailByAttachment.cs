using System;
using System.Collections.Generic;
using System.Linq;
using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class EmailByAttachment : MapIndex
    {
        public DateTime Date { get; set; }
        public string AttachmentName { get; set; }
    }

    public class EmailByAttachmentProvider : IndexProvider<Email>
    {
        public override void Describe(DescribeContext<Email> context)
        {
            context.For<EmailByAttachment>()
                .Map(e => e.Attachments.Select(a => new EmailByAttachment()
                {
                    Date = e.Date,
                    AttachmentName = a.Name
                }));
        }
    }
}
