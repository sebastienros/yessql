using System;
using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class WorkflowIndex : MapIndex
    {
        public DateTime CreatedUtc { get; set; }

        public WorkflowStatus WorkflowStatus { get; set; }
    }

    public class WorkflowIndexProvider : IndexProvider<Workflow>
    {
        public override void Describe(DescribeContext<Workflow> context)
        {
            context
                .For<WorkflowIndex>()
                .Map(workflow => new WorkflowIndex
                {
                    CreatedUtc = workflow.CreatedUtc,
                    WorkflowStatus = workflow.WorkflowStatus
                });
        }
    }
}
