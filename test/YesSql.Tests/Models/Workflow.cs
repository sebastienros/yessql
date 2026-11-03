using System;

namespace YesSql.Tests.Models
{
    public class Workflow
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedUtc { get; set; }
        public WorkflowStatus WorkflowStatus { get; set; }
    }

    public enum WorkflowStatus
    {
        Draft,
        Running,
        Completed
    }
}
